using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class CreateBillCommand : IRequest<object>
    {
        public CreateBillModel Model { get; }

        public CreateBillCommand(CreateBillModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateBillCommand, object>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IContextAccessor _contextAccessor;
            private readonly IStringLocalizer<CreateBillCommand> _localizer;
            private readonly IAppointmentStateMachine _appointmentStateMachine;
            private readonly ICacheService _cacheService;
            private readonly VnPaySettings _vnpaySettings;
            private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);
            private const int DefaultExpireMinutes = 15;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                Func<IContextAccessor> contextAccessorFactory,
                IStringLocalizer<CreateBillCommand> localizer,
                ICacheService cacheService,
                IAppointmentStateMachine appointmentStateMachine,
                IOptions<VnPaySettings> vnpayOptions)
            {
                _dataContext = dataContext;
                _contextAccessor = contextAccessorFactory();
                _localizer = localizer;
                _cacheService = cacheService;
                _appointmentStateMachine = appointmentStateMachine;
                _vnpaySettings = vnpayOptions.Value;
            }

            public async Task<object> Handle(CreateBillCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                var currentUserId = _contextAccessor.UserId;
                var currentUserRole = _contextAccessor.Role;

                if (currentUserRole != Role.RECEPTIONIST.ToString() && currentUserRole != Role.CUSTOMER.ToString())
                {
                    throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
                }

                if (!Enum.TryParse<PaymentMethod>(model.PaymentMethod, true, out var paymentMethod))
                {
                    throw new ArgumentException(_localizer["payment.method.invalid"]);
                }

                var invoiceDetails = await (from invoice in _dataContext.VcInvoices
                                          join appt in _dataContext.VcAppointments on invoice.AppointmentId equals appt.Id
                                          where model.AppointmentIds.Contains(invoice.AppointmentId) && invoice.IsActive
                                          select new 
                                          {
                                              Invoice = invoice,
                                              Appointment = appt
                                          }).ToListAsync(cancellationToken);

                if (invoiceDetails.Count != model.AppointmentIds.Count)
                {
                    throw new ArgumentException(_localizer["invoice.some_not_found"]);
                }

                if (invoiceDetails.Any(id => id.Invoice.Status == PaymentStatus.PAID.ToString() ||
                                             id.Invoice.Status == PaymentStatus.SUCCESS.ToString()))
                {
                    throw new ArgumentException(_localizer["invoice.already_processed"]);
                }

                var linkedBillIds = invoiceDetails
                    .Where(id => id.Invoice.BillId.HasValue)
                    .Select(id => id.Invoice.BillId!.Value)
                    .Distinct()
                    .ToList();

                if (linkedBillIds.Count > 1)
                {
                    throw new ArgumentException(_localizer["invoice.already_processed"]);
                }
                
                var customerId = invoiceDetails.First().Appointment.CustomerId;
                if (invoiceDetails.Any(id => id.Appointment.CustomerId != customerId))
                {
                    throw new ArgumentException(_localizer["bill.customer.mismatch"]);
                }

                await using var transaction = await _dataContext.Database.BeginTransactionAsync(cancellationToken);

                var invoices = invoiceDetails.Select(id => id.Invoice).ToList();
                var appointments = invoiceDetails.Select(id => id.Appointment).ToList();
                var totalAmount = invoices.Sum(i => i.TotalAmount);
                VcBills bill;

                if (linkedBillIds.Count == 1)
                {
                    bill = await _dataContext.VcBills
                        .FirstOrDefaultAsync(b => b.Id == linkedBillIds[0] && b.IsActive, cancellationToken);

                    if (bill == null)
                    {
                        foreach (var invoice in invoices.Where(i => i.BillId.HasValue))
                        {
                            invoice.BillId = null;
                            invoice.Status = PaymentStatus.PENDING.ToString();
                        }

                        bill = await CreateNewBill(customerId, totalAmount, model.Note, currentUserId, cancellationToken);
                    }
                    else
                    {
                        if (bill.CustomerId != customerId)
                        {
                            throw new ArgumentException(_localizer["bill.customer.mismatch"]);
                        }

                        if (string.Equals(bill.Status, PaymentStatus.PAID.ToString(), StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(bill.Status, PaymentStatus.SUCCESS.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            throw new ArgumentException(_localizer["invoice.already_processed"]);
                        }

                        foreach (var invoice in invoices)
                        {
                            invoice.BillId = bill.Id;
                            invoice.Status = PaymentStatus.PENDING.ToString();
                        }

                        bill.TotalAmount = invoices.Sum(i => i.TotalAmount);
                        bill.Status = PaymentStatus.PENDING.ToString();
                        bill.Note = model.Note ?? bill.Note;
                    }
                }
                else
                {
                    bill = await CreateNewBill(customerId, totalAmount, model.Note, currentUserId, cancellationToken);
                }

                foreach (var invoice in invoices)
                {
                    invoice.BillId = bill.Id;
                }
                
                var payment = new VcPayments
                {
                    BillId = bill.Id,
                    Code = GenerateCodeUtils.GenerateUserCode("PAY"),
                    PaymentMethod = paymentMethod.ToString(),
                    PaymentStatus = PaymentStatus.PENDING.ToString(),
                    Amount = totalAmount,
                    CreatedUserId = currentUserId,
                    CreatedUserName = _contextAccessor.UserName
                };
                await _dataContext.VcPayments.AddAsync(payment, cancellationToken);
                
                await _dataContext.SaveChangesAsync(cancellationToken);
                
                if (paymentMethod == PaymentMethod.CASH)
                {
                    bill.Status = PaymentStatus.PAID.ToString();
                    payment.PaymentStatus = PaymentStatus.SUCCESS.ToString();
                    payment.PaymentDate = DateTime.UtcNow;
                    
                    foreach (var invoice in invoices)
                    {
                        invoice.Status = PaymentStatus.PAID.ToString();
                        invoice.PaidDate = DateTime.UtcNow;
                    }

                    foreach (var appt in appointments)
                    {
                        _appointmentStateMachine.Apply(appt, AppointmentAction.CASH_PAYMENT);
                        appt.StateName = _appointmentStateMachine.GetStateDisplayName(Enum.Parse<AppointmentStatus>(appt.State));
                    }
                    
                    await _dataContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    Log.Information("Bill {BillId} for customer {CustomerId} created and paid by CASH.", bill.Id, customerId);
                    return bill;
                }
                
                if (paymentMethod == PaymentMethod.VNPAY)
                {
                    await transaction.CommitAsync(cancellationToken);
                    var paymentUrl = CreatePaymentUrl(totalAmount, payment, invoices.Count, model.ClientIpAddress);
                    Log.Information("Bill {BillId} for customer {CustomerId} created for VNPAY. URL generated.", bill.Id, customerId);
                    return new VnPayPaymentUrlModel
                    {
                        PaymentUrl = paymentUrl,
                        PaymentId = payment.Id,
                        Amount = totalAmount,
                        InvoiceCount = invoices.Count
                    };
                }

                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException("Unsupported payment method.");
            }

            private async Task<VcBills> CreateNewBill(
                int customerId,
                decimal totalAmount,
                string? note,
                int? currentUserId,
                CancellationToken cancellationToken)
            {
                var bill = new VcBills
                {
                    Code = GenerateCodeUtils.GenerateUserCode("BILL"),
                    CustomerId = customerId,
                    TotalAmount = totalAmount,
                    Status = PaymentStatus.PENDING.ToString(),
                    BillDate = DateTime.UtcNow,
                    Note = note,
                    CreatedUserId = currentUserId,
                    CreatedUserName = _contextAccessor.UserName
                };

                await _dataContext.VcBills.AddAsync(bill, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                return bill;
            }
            
            private string CreatePaymentUrl(decimal totalAmount, VcPayments payment, int invoiceCount, string? clientIpAddress)
            {
                var vnpay = new VnPayLibrary();
                var now = DateTime.UtcNow.Add(VietnamUtcOffset);
                var expireDate = now.AddMinutes(_vnpaySettings.ExpireMinutes > 0 ? _vnpaySettings.ExpireMinutes : DefaultExpireMinutes);
                var amount = decimal.ToInt64(decimal.Round(totalAmount * 100, 0, MidpointRounding.AwayFromZero));

                vnpay.AddRequestData("vnp_Version", _vnpaySettings.Version);
                vnpay.AddRequestData("vnp_Command", _vnpaySettings.Command);
                vnpay.AddRequestData("vnp_TmnCode", _vnpaySettings.TmnCode);
                vnpay.AddRequestData("vnp_Amount", amount.ToString(CultureInfo.InvariantCulture));
                vnpay.AddRequestData("vnp_CreateDate", now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
                vnpay.AddRequestData("vnp_CurrCode", _vnpaySettings.CurrCode);
                vnpay.AddRequestData("vnp_IpAddr", NormalizeClientIpAddress(clientIpAddress));
                vnpay.AddRequestData("vnp_Locale", _vnpaySettings.Locale);
                vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan hoa don {payment.Code}");
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", _vnpaySettings.ReturnUrl);
                vnpay.AddRequestData("vnp_TxnRef", payment.Id.ToString(CultureInfo.InvariantCulture));
                vnpay.AddRequestData("vnp_ExpireDate", expireDate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));

                return vnpay.CreateRequestUrl(_vnpaySettings.BaseUrl, _vnpaySettings.HashSecret);
            }

            private static string NormalizeClientIpAddress(string? clientIpAddress)
            {
                if (string.IsNullOrWhiteSpace(clientIpAddress)) return "127.0.0.1";
                if (!IPAddress.TryParse(clientIpAddress, out var ipAddress)) return "127.0.0.1";
                if (ipAddress.IsIPv4MappedToIPv6) return ipAddress.MapToIPv4().ToString();
                return IPAddress.IsLoopback(ipAddress) ? "127.0.0.1" : ipAddress.ToString();
            }
        }
    }
}
