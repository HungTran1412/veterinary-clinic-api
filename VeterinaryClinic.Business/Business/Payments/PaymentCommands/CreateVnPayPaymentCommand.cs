using System.Globalization;
using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Serilog;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.Business
{
    public class CreateVnPayPaymentCommand : IRequest<VnPayPaymentUrlModel>
    {
        public CreateVnPayPaymentModel Model { get; }

        public CreateVnPayPaymentCommand(CreateVnPayPaymentModel model)
        {
            Model = model;
        }

        public class Handler : IRequestHandler<CreateVnPayPaymentCommand, VnPayPaymentUrlModel>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IStringLocalizer<CreateVnPayPaymentCommand> _localizer;
            private readonly IContextAccessor _contextAccessor;
            private readonly VnPaySettings _vnpaySettings;
            private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);
            private const int DefaultExpireMinutes = 15;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                IStringLocalizer<CreateVnPayPaymentCommand> localizer,
                Func<IContextAccessor> contextAccessorFactory,
                IOptions<VnPaySettings> vnpayOptions)
            {
                _dataContext = dataContext;
                _localizer = localizer;
                _contextAccessor = contextAccessorFactory();
                _vnpaySettings = vnpayOptions.Value;
            }

            public async Task<VnPayPaymentUrlModel> Handle(CreateVnPayPaymentCommand request, CancellationToken cancellationToken)
            {
                var model = request.Model;
                var appointment = await _dataContext.VcAppointments
                    .FirstOrDefaultAsync(x => x.Id == model.AppointmentId && x.IsActive, cancellationToken);
                if (appointment == null)
                {
                    throw new ArgumentException(_localizer["appointment.not_found"]);
                }

                if (_contextAccessor.Role == Role.CUSTOMER.ToString() && appointment.CustomerId != _contextAccessor.UserId)
                {
                    throw new UnauthorizedAccessException(_localizer["user.unauthorized"]);
                }

                if (appointment.State != AppointmentStatus.PAYMENT_PENDING.ToString())
                {
                    throw new ArgumentException(_localizer["appointment.payment.not_pending"]);
                }

                var pendingInvoices = await (
                    from invoice in _dataContext.VcInvoices
                    join appt in _dataContext.VcAppointments on invoice.AppointmentId equals appt.Id
                    where invoice.IsActive &&
                          appt.IsActive &&
                          appt.CustomerId == appointment.CustomerId &&
                          invoice.Status != PaymentStatus.SUCCESS.ToString() &&
                          appt.State == AppointmentStatus.PAYMENT_PENDING.ToString()
                    select new { Invoice = invoice, Appointment = appt })
                    .ToListAsync(cancellationToken);

                if (!pendingInvoices.Any())
                {
                    throw new ArgumentException(_localizer["invoice.not_found"]);
                }

                if (pendingInvoices.Any(x => x.Invoice.TotalAmount <= 0))
                {
                    throw new ArgumentException(_localizer["invoice.amount.invalid"]);
                }

                var paymentBatchCode = GenerateCodeUtils.GenerateUserCode("PAY");
                var payments = new List<VcPayments>();

                foreach (var item in pendingInvoices)
                {
                    item.Invoice.Status = PaymentStatus.PENDING.ToString();

                    payments.Add(new VcPayments
                    {
                        InvoiceId = item.Invoice.Id,
                        Code = paymentBatchCode,
                        PaymentMethod = PaymentMethod.VNPAY.ToString(),
                        PaymentStatus = PaymentStatus.PENDING.ToString(),
                        Amount = item.Invoice.TotalAmount,
                        GatewayTransactionId = null,
                        ResponseCode = null,
                        GatewayResponse = null,
                        PaymentDate = null,
                        IsActive = true,
                        Order = 0,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUserId = _contextAccessor.UserId,
                        CreatedUserName = _contextAccessor.UserName
                    });
                }

                await _dataContext.VcPayments.AddRangeAsync(payments, cancellationToken);
                await _dataContext.SaveChangesAsync(cancellationToken);

                var anchorPayment = payments.First();
                var totalAmount = payments.Sum(x => x.Amount);
                var paymentUrl = CreatePaymentUrl(totalAmount, anchorPayment, pendingInvoices.Count, model.ClientIpAddress);
                Log.Information(
                    "Created VNPay payment url for Customer {CustomerId}, Payment {PaymentId}, InvoiceCount {InvoiceCount}",
                    appointment.CustomerId,
                    anchorPayment.Id,
                    pendingInvoices.Count);

                return new VnPayPaymentUrlModel
                {
                    PaymentUrl = paymentUrl,
                    InvoiceId = anchorPayment.InvoiceId,
                    PaymentId = anchorPayment.Id,
                    Amount = totalAmount,
                    InvoiceCount = pendingInvoices.Count
                };
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
                vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan {invoiceCount} hoa don");
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", _vnpaySettings.ReturnUrl);
                vnpay.AddRequestData("vnp_TxnRef", payment.Id.ToString(CultureInfo.InvariantCulture));
                vnpay.AddRequestData("vnp_ExpireDate", expireDate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));

                return vnpay.CreateRequestUrl(_vnpaySettings.BaseUrl, _vnpaySettings.HashSecret);
            }

            private static string NormalizeClientIpAddress(string? clientIpAddress)
            {
                if (string.IsNullOrWhiteSpace(clientIpAddress))
                {
                    return "127.0.0.1";
                }

                if (!IPAddress.TryParse(clientIpAddress, out var ipAddress))
                {
                    return "127.0.0.1";
                }

                if (ipAddress.IsIPv4MappedToIPv6)
                {
                    return ipAddress.MapToIPv4().ToString();
                }

                return IPAddress.IsLoopback(ipAddress) ? "127.0.0.1" : ipAddress.ToString();
            }
        }
    }
}
