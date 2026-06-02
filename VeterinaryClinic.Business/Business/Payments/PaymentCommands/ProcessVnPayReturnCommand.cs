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
            private readonly IAppointmentStateMachine _appointmentStateMachine;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                IStringLocalizer<ProcessVnPayReturnCommand> localizer,
                IOptions<VnPaySettings> vnpayOptions,
                IAppointmentStateMachine appointmentStateMachine)
            {
                _dataContext = dataContext;
                _localizer = localizer;
                _vnpaySettings = vnpayOptions.Value;
                _appointmentStateMachine = appointmentStateMachine;
            }

            public async Task<VnPayReturnModel> Handle(ProcessVnPayReturnCommand request, CancellationToken cancellationToken)
            {
                // 1. Validate signature from VNPay
                var vnpay = new VnPayLibrary();
                foreach (var (key, value) in request.QueryData)
                {
                    if (key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                    {
                        vnpay.AddResponseData(key, value);
                    }
                }

                var secureHash = request.QueryData.GetValueOrDefault("vnp_SecureHash");
                if (string.IsNullOrWhiteSpace(secureHash) || !vnpay.ValidateSignature(secureHash, _vnpaySettings.HashSecret))
                {
                    throw new ArgumentException(_localizer["payment.vnpay.invalid_signature"]);
                }

                // 2. Find the payment record
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

                // 3. Process payment result
                var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
                var transactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                var gatewayTransactionId = vnpay.GetResponseData("vnp_TransactionNo");
                var isSuccess = responseCode == "00" && transactionStatus == "00";
                var paymentStatus = isSuccess ? PaymentStatus.SUCCESS.ToString() : PaymentStatus.FAILED.ToString();
                var invoiceAndBillStatus = isSuccess ? PaymentStatus.PAID.ToString() : PaymentStatus.FAILED.ToString();
                var now = DateTime.UtcNow;

                // Update the primary payment record
                payment.PaymentStatus = paymentStatus;
                payment.ResponseCode = responseCode;
                payment.GatewayTransactionId = gatewayTransactionId;
                payment.GatewayResponse = JsonSerializer.Serialize(request.QueryData);
                payment.PaymentDate = now;

                if (payment.BillId.HasValue)
                {
                    // --- NEW LOGIC: Processing a master Bill ---
                    var bill = await _dataContext.VcBills.FindAsync(payment.BillId.Value);
                    if (bill == null) throw new KeyNotFoundException(_localizer["bill.not_found"]);

                    // Validate amount
                    if (!long.TryParse(vnpay.GetResponseData("vnp_Amount"), out var vnpAmountValue) || vnpAmountValue / 100m != bill.TotalAmount)
                    {
                        bill.Status = PaymentStatus.FAILED.ToString();
                        await _dataContext.SaveChangesAsync(cancellationToken);
                        throw new ArgumentException(_localizer["payment.vnpay.invalid_amount"]);
                    }
                    
                    // Update statuses
                    bill.Status = invoiceAndBillStatus;
                    
                    var invoices = await _dataContext.VcInvoices.Where(i => i.BillId == bill.Id).ToListAsync(cancellationToken);
                    var appointmentIds = invoices.Select(i => i.AppointmentId).ToList();
                    var appointments = await _dataContext.VcAppointments.Where(a => appointmentIds.Contains(a.Id)).ToListAsync(cancellationToken);

                    foreach (var invoice in invoices)
                    {
                        invoice.Status = invoiceAndBillStatus;
                        if(isSuccess) invoice.PaidDate = now;
                    }

                    if (isSuccess)
                    {
                        foreach (var appt in appointments)
                        {
                            _appointmentStateMachine.ApplySystem(appt, AppointmentAction.BANK_TRANSFER);
                            appt.StateName = _appointmentStateMachine.GetStateDisplayName(Enum.Parse<AppointmentStatus>(appt.State));
                        }
                    }
                }
                else if (payment.InvoiceId.HasValue)
                {
                    // --- OLD LOGIC: Processing a single Invoice ---
                    var invoice = await _dataContext.VcInvoices.FindAsync(payment.InvoiceId.Value);
                    if (invoice == null) throw new KeyNotFoundException(_localizer["invoice.not_found"]);

                    // Validate amount
                    if (!long.TryParse(vnpay.GetResponseData("vnp_Amount"), out var vnpAmountValue) || vnpAmountValue / 100m != invoice.TotalAmount)
                    {
                        invoice.Status = PaymentStatus.FAILED.ToString();
                        await _dataContext.SaveChangesAsync(cancellationToken);
                        throw new ArgumentException(_localizer["payment.vnpay.invalid_amount"]);
                    }
                    
                    // Update statuses
                    invoice.Status = invoiceAndBillStatus;
                    if(isSuccess) invoice.PaidDate = now;

                    if (isSuccess)
                    {
                        var appointment = await _dataContext.VcAppointments.FindAsync(invoice.AppointmentId);
                        if (appointment != null)
                        {
                            _appointmentStateMachine.ApplySystem(appointment, AppointmentAction.BANK_TRANSFER);
                            appointment.StateName = _appointmentStateMachine.GetStateDisplayName(Enum.Parse<AppointmentStatus>(appointment.State));
                        }
                    }
                }
                
                await _dataContext.SaveChangesAsync(cancellationToken);

                return new VnPayReturnModel
                {
                    IsSuccess = isSuccess,
                    ResponseCode = responseCode,
                    Message = isSuccess ? "Payment success" : "Payment failed",
                    PaymentId = payment.Id,
                    Amount = payment.Amount
                };
            }
        }
    }
}
