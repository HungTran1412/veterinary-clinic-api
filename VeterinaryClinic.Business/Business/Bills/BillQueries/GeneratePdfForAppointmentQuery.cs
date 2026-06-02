using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    /// <summary>
    /// Query "thông minh" để tạo PDF từ một lịch hẹn.
    /// Nếu invoice đã thuộc bill thì in theo bill; nếu chưa thì sinh PDF lẻ mà không làm thay đổi dữ liệu thanh toán.
    /// </summary>
    public class GeneratePdfForAppointmentQuery : IRequest<byte[]>
    {
        public int AppointmentId { get; }

        public GeneratePdfForAppointmentQuery(int appointmentId)
        {
            AppointmentId = appointmentId;
        }

        public class Handler : IRequestHandler<GeneratePdfForAppointmentQuery, byte[]>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IMediator _mediator;
            private readonly IStringLocalizer<GeneratePdfForAppointmentQuery> _localizer;
            private readonly IPdfService _pdfService;

            public Handler(
                VeterinaryClinicDataContext dataContext,
                IMediator mediator,
                IStringLocalizer<GeneratePdfForAppointmentQuery> localizer,
                IPdfService pdfService)
            {
                _dataContext = dataContext;
                _mediator = mediator;
                _localizer = localizer;
                _pdfService = pdfService;
            }

            public async Task<byte[]> Handle(GeneratePdfForAppointmentQuery request, CancellationToken cancellationToken)
            {
                // 1. Find the single invoice associated with the appointment
                var invoice = await _dataContext.VcInvoices
                    .FirstOrDefaultAsync(i => i.AppointmentId == request.AppointmentId, cancellationToken);

                if (invoice == null)
                {
                    throw new KeyNotFoundException(_localizer["invoice.not_found"]);
                }

                int billId;

                // 2. Check if this invoice is already part of a larger bill
                if (invoice.BillId.HasValue)
                {
                    // Case A: It's part of a group. Just use the existing BillId.
                    billId = invoice.BillId.Value;
                }
                else
                {
                    return await GenerateSingleInvoicePdf(request.AppointmentId, invoice.TotalAmount, invoice.PaidDate, cancellationToken);
                }

                // 3. Now that we have a guaranteed billId, call the main PDF generation query
                return await _mediator.Send(new GenerateBillPdfQuery(billId), cancellationToken);
            }

            private async Task<byte[]> GenerateSingleInvoicePdf(
                int appointmentId,
                decimal totalAmount,
                DateTime paidDate,
                CancellationToken cancellationToken)
            {
                var appointmentData = await (
                    from appt in _dataContext.VcAppointments
                    join customer in _dataContext.VcUsers on appt.CustomerId equals customer.Id
                    join service in _dataContext.VcServices on appt.ServiceId equals service.Id
                    join pet in _dataContext.VcPets on appt.PetId equals pet.Id
                    where appt.Id == appointmentId
                    select new
                    {
                        Appointment = appt,
                        Customer = customer,
                        Service = service,
                        Pet = pet
                    }).FirstOrDefaultAsync(cancellationToken);

                if (appointmentData == null)
                {
                    throw new KeyNotFoundException(_localizer["appointment.not_found"]);
                }

                var pdfModel = new BillPdfModel
                {
                    BillCode = $"INV-{appointmentData.Appointment.Code}",
                    BillDate = paidDate != default ? paidDate : DateTime.UtcNow,
                    CustomerName = appointmentData.Customer.FullName,
                    CustomerPhone = appointmentData.Customer.PhoneNumber,
                    CustomerAddress = appointmentData.Customer.Address ?? string.Empty,
                    Items = new List<BillPdfItemModel>
                    {
                        new()
                        {
                            Index = 1,
                            ServiceName = appointmentData.Service.Name,
                            PetName = appointmentData.Pet.Name,
                            Price = totalAmount
                        }
                    },
                    TotalAmount = totalAmount
                };

                return _pdfService.GenerateBillPdf(pdfModel);
            }
        }
    }
}
