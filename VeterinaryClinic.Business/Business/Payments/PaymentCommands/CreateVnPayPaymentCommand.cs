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

                var invoice = await _dataContext.VcInvoices
                    .FirstOrDefaultAsync(x => x.AppointmentId == appointment.Id && x.IsActive, cancellationToken);
                if (invoice == null)
                {
                    throw new ArgumentException(_localizer["invoice.not_found"]);
                }

                if (invoice.TotalAmount <= 0)
                {
                    throw new ArgumentException(_localizer["invoice.amount.invalid"]);
                }

                if (invoice.Status == PaymentStatus.SUCCESS.ToString())
                {
                    throw new ArgumentException(_localizer["invoice.already_paid"]);
                }

                var payment = await _dataContext.VcPayments
                    .FirstOrDefaultAsync(x =>
                        x.InvoiceId == invoice.Id &&
                        x.PaymentMethod == PaymentMethod.VNPAY.ToString() &&
                        x.PaymentStatus == PaymentStatus.PENDING.ToString() &&
                        x.IsActive,
                        cancellationToken);

                if (payment == null)
                {
                    payment = new VcPayments
                    {
                        InvoiceId = invoice.Id,
                        Code = GenerateCodeUtils.GenerateUserCode("PAY"),
                        PaymentMethod = PaymentMethod.VNPAY.ToString(),
                        PaymentStatus = PaymentStatus.PENDING.ToString(),
                        Amount = invoice.TotalAmount,
                        GatewayTransactionId = null,
                        ResponseCode = null,
                        GatewayResponse = null,
                        PaymentDate = null,
                        IsActive = true,
                        Order = 0,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUserId = _contextAccessor.UserId,
                        CreatedUserName = _contextAccessor.UserName
                    };

                    await _dataContext.VcPayments.AddAsync(payment, cancellationToken);
                    await _dataContext.SaveChangesAsync(cancellationToken);
                }

                invoice.Status = PaymentStatus.PENDING.ToString();
                await _dataContext.SaveChangesAsync(cancellationToken);

                var paymentUrl = CreatePaymentUrl(invoice, payment, model.ClientIpAddress);
                Log.Information("Created VNPay payment url for Invoice {InvoiceId}, Payment {PaymentId}", invoice.Id, payment.Id);

                return new VnPayPaymentUrlModel
                {
                    PaymentUrl = paymentUrl,
                    InvoiceId = invoice.Id,
                    PaymentId = payment.Id,
                    Amount = invoice.TotalAmount
                };
            }

            private string CreatePaymentUrl(VcInvoices invoice, VcPayments payment, string? clientIpAddress)
            {
                var vnpay = new VnPayLibrary();
                var now = DateTime.UtcNow;

                vnpay.AddRequestData("vnp_Version", _vnpaySettings.Version);
                vnpay.AddRequestData("vnp_Command", _vnpaySettings.Command);
                vnpay.AddRequestData("vnp_TmnCode", _vnpaySettings.TmnCode);
                vnpay.AddRequestData("vnp_Amount", ((long)(invoice.TotalAmount * 100)).ToString());
                vnpay.AddRequestData("vnp_CreateDate", now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", _vnpaySettings.CurrCode);
                vnpay.AddRequestData("vnp_IpAddr", string.IsNullOrWhiteSpace(clientIpAddress) ? "127.0.0.1" : clientIpAddress);
                vnpay.AddRequestData("vnp_Locale", _vnpaySettings.Locale);
                vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan hoa don {invoice.Code}");
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", _vnpaySettings.ReturnUrl);
                vnpay.AddRequestData("vnp_TxnRef", payment.Id.ToString());

                return vnpay.CreateRequestUrl(_vnpaySettings.BaseUrl, _vnpaySettings.HashSecret);
            }
        }
    }
}
