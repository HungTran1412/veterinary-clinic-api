using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class ProcessVnPayReturnCommand : IRequest<VnPayReturnModel>
    {
        public Dictionary<string, string> QueryData { get; }

        public ProcessVnPayReturnCommand(Dictionary<string, string> queryData)
        {
            QueryData = queryData;
        }

        public class Handler : IRequestHandler<ProcessVnPayReturnCommand, VnPayReturnModel>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IStringLocalizer<ProcessVnPayReturnCommand> _localizer;
            private readonly VnPaySettings _vnpaySettings;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                IStringLocalizer<ProcessVnPayReturnCommand> localizer,
                IOptions<VnPaySettings> vnpayOptions)
            {
                _dataContext = dataContext;
                _localizer = localizer;
                _vnpaySettings = vnpayOptions.Value;
            }

            public async Task<VnPayReturnModel> Handle(ProcessVnPayReturnCommand request, CancellationToken cancellationToken)
            {
                if (request.QueryData.Count == 0)
                {
                    throw new ArgumentException(_localizer["payment.vnpay.input_required"]);
                }

                var vnpay = new VnPayLibrary();
                foreach (var item in request.QueryData)
                {
                    if (item.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                    {
                        vnpay.AddResponseData(item.Key, item.Value);
                    }
                }

                var secureHash = request.QueryData.GetValueOrDefault("vnp_SecureHash");
                if (string.IsNullOrWhiteSpace(secureHash) || !vnpay.ValidateSignature(secureHash, _vnpaySettings.HashSecret))
                {
                    throw new ArgumentException(_localizer["payment.vnpay.invalid_signature"]);
                }

                if (!int.TryParse(vnpay.GetResponseData("vnp_TxnRef"), out var paymentId))
                {
                    throw new ArgumentException(_localizer["payment.not_found"]);
                }

                var payment = await _dataContext.VcPayments
                    .FirstOrDefaultAsync(x => x.Id == paymentId && x.IsActive, cancellationToken);
                if (payment == null)
                {
                    throw new ArgumentException(_localizer["payment.not_found"]);
                }

                var invoice = await _dataContext.VcInvoices
                    .FirstOrDefaultAsync(x => x.Id == payment.InvoiceId && x.IsActive, cancellationToken);
                if (invoice == null)
                {
                    throw new ArgumentException(_localizer["invoice.not_found"]);
                }

                var appointment = await _dataContext.VcAppointments
                    .FirstOrDefaultAsync(x => x.Id == invoice.AppointmentId && x.IsActive, cancellationToken);

                var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
                var transactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                var gatewayTransactionId = vnpay.GetResponseData("vnp_TransactionNo");

                if (!long.TryParse(vnpay.GetResponseData("vnp_Amount"), out var vnpAmountValue) ||
                    vnpAmountValue / 100m != invoice.TotalAmount)
                {
                    payment.PaymentStatus = PaymentStatus.FAILED.ToString();
                    payment.ResponseCode = responseCode;
                    payment.GatewayTransactionId = gatewayTransactionId;
                    payment.GatewayResponse = JsonSerializer.Serialize(request.QueryData);
                    invoice.Status = PaymentStatus.FAILED.ToString();
                    await _dataContext.SaveChangesAsync(cancellationToken);

                    throw new ArgumentException(_localizer["payment.vnpay.invalid_amount"]);
                }

                var isSuccess = responseCode == "00" && transactionStatus == "00";
                payment.PaymentStatus = isSuccess ? PaymentStatus.SUCCESS.ToString() : PaymentStatus.FAILED.ToString();
                payment.PaymentMethod = PaymentMethod.VNPAY.ToString();
                payment.Amount = invoice.TotalAmount;
                payment.ResponseCode = responseCode;
                payment.GatewayTransactionId = gatewayTransactionId;
                payment.GatewayResponse = JsonSerializer.Serialize(request.QueryData);
                payment.PaymentDate = DateTime.UtcNow;

                invoice.Status = payment.PaymentStatus;
                if (isSuccess)
                {
                    invoice.PaidDate = DateTime.UtcNow;

                    if (appointment != null)
                    {
                        appointment.State = AppointmentStatus.COMPLETED.ToString();
                        appointment.StateName = "Hoan thanh";
                        appointment.IsFinalState = true;
                    }
                }

                await _dataContext.SaveChangesAsync(cancellationToken);

                return new VnPayReturnModel
                {
                    IsSuccess = isSuccess,
                    ResponseCode = responseCode,
                    Message = isSuccess ? "Payment success" : "Payment failed",
                    InvoiceId = invoice.Id,
                    PaymentId = payment.Id,
                    AppointmentId = appointment?.Id
                };
            }
        }
    }
}
