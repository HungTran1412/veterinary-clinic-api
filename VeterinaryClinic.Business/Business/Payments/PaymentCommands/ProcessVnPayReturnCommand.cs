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

                var batchPayments = await _dataContext.VcPayments
                    .Where(x => x.IsActive && x.Code == payment.Code)
                    .ToListAsync(cancellationToken);
                if (!batchPayments.Any())
                {
                    throw new ArgumentException(_localizer["payment.not_found"]);
                }

                var invoiceIds = batchPayments.Select(x => x.InvoiceId).Distinct().ToList();
                var invoices = await _dataContext.VcInvoices
                    .Where(x => x.IsActive && invoiceIds.Contains(x.Id))
                    .ToListAsync(cancellationToken);
                if (!invoices.Any())
                {
                    throw new ArgumentException(_localizer["invoice.not_found"]);
                }

                var appointmentIds = invoices.Select(x => x.AppointmentId).Distinct().ToList();
                var appointments = await _dataContext.VcAppointments
                    .Where(x => x.IsActive && appointmentIds.Contains(x.Id))
                    .ToListAsync(cancellationToken);

                var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
                var transactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                var gatewayTransactionId = vnpay.GetResponseData("vnp_TransactionNo");
                var totalAmount = invoices.Sum(x => x.TotalAmount);
                var hasPaidInvoice = invoices.Any(x => x.Status == PaymentStatus.SUCCESS.ToString());

                if (!long.TryParse(vnpay.GetResponseData("vnp_Amount"), out var vnpAmountValue) ||
                    vnpAmountValue / 100m != totalAmount)
                {
                    foreach (var batchPayment in batchPayments)
                    {
                        batchPayment.PaymentStatus = PaymentStatus.FAILED.ToString();
                        batchPayment.ResponseCode = responseCode;
                        batchPayment.GatewayTransactionId = gatewayTransactionId;
                        batchPayment.GatewayResponse = JsonSerializer.Serialize(request.QueryData);
                    }

                    foreach (var invoice in invoices.Where(x => x.Status != PaymentStatus.SUCCESS.ToString()))
                    {
                        invoice.Status = PaymentStatus.FAILED.ToString();
                    }

                    await _dataContext.SaveChangesAsync(cancellationToken);

                    throw new ArgumentException(_localizer["payment.vnpay.invalid_amount"]);
                }

                var isSuccess = responseCode == "00" && transactionStatus == "00";
                var paymentStatus = isSuccess ? PaymentStatus.SUCCESS.ToString() : PaymentStatus.FAILED.ToString();
                var now = DateTime.UtcNow;

                foreach (var batchPayment in batchPayments)
                {
                    var matchedInvoice = invoices.First(x => x.Id == batchPayment.InvoiceId);
                    batchPayment.PaymentStatus = paymentStatus;
                    batchPayment.PaymentMethod = PaymentMethod.VNPAY.ToString();
                    batchPayment.Amount = matchedInvoice.TotalAmount;
                    batchPayment.ResponseCode = responseCode;
                    batchPayment.GatewayTransactionId = gatewayTransactionId;
                    batchPayment.GatewayResponse = JsonSerializer.Serialize(request.QueryData);
                    batchPayment.PaymentDate = now;
                }

                foreach (var invoice in invoices)
                {
                    if (isSuccess || invoice.Status != PaymentStatus.SUCCESS.ToString())
                    {
                        invoice.Status = paymentStatus;
                    }

                    if (isSuccess)
                    {
                        invoice.PaidDate = now;
                    }
                }

                if (isSuccess)
                {
                    foreach (var appointment in appointments.Where(x => x.State == AppointmentStatus.PAYMENT_PENDING.ToString()))
                    {
                        appointment.State = AppointmentStatus.COMPLETED.ToString();
                        appointment.StateName = "Hoan thanh";
                        appointment.IsFinalState = true;
                        appointment.ModifiedDate = now;
                    }
                }

                await _dataContext.SaveChangesAsync(cancellationToken);

                return new VnPayReturnModel
                {
                    IsSuccess = isSuccess,
                    ResponseCode = responseCode,
                    Message = isSuccess ? "Payment success" : "Payment failed",
                    InvoiceId = invoices.FirstOrDefault()?.Id,
                    PaymentId = payment.Id,
                    AppointmentId = appointments.FirstOrDefault()?.Id,
                    Amount = totalAmount,
                    InvoiceCount = invoices.Count
                };
            }
        }
    }
}
