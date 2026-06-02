using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class GenerateBillPdfQuery : IRequest<byte[]>
    {
        public int BillId { get; }

        public GenerateBillPdfQuery(int billId)
        {
            BillId = billId;
        }

        public class Handler : IRequestHandler<GenerateBillPdfQuery, byte[]>
        {
            private readonly VeterinaryClinicDataContext _dataContext;
            private readonly IPdfService _pdfService;
            private readonly IStringLocalizer<GenerateBillPdfQuery> _localizer;

            public Handler(VeterinaryClinicDataContext dataContext, IPdfService pdfService, IStringLocalizer<GenerateBillPdfQuery> localizer)
            {
                _dataContext = dataContext;
                _pdfService = pdfService;
                _localizer = localizer;
            }

            public async Task<byte[]> Handle(GenerateBillPdfQuery request, CancellationToken cancellationToken)
            {
                // 1. Get the Bill and Customer info
                var billAndCustomer = await (from bill in _dataContext.VcBills
                                             join customer in _dataContext.VcUsers on bill.CustomerId equals customer.Id
                                             where bill.Id == request.BillId
                                             select new
                                             {
                                                 Bill = bill,
                                                 Customer = customer
                                             }).FirstOrDefaultAsync(cancellationToken);

                if (billAndCustomer == null)
                {
                    throw new KeyNotFoundException(_localizer["bill.not_found"]);
                }

                // 2. Get all items (services) in the bill
                var billItems = await (from invoice in _dataContext.VcInvoices
                                       join appt in _dataContext.VcAppointments on invoice.AppointmentId equals appt.Id
                                       join service in _dataContext.VcServices on appt.ServiceId equals service.Id
                                       join pet in _dataContext.VcPets on appt.PetId equals pet.Id
                                       where invoice.BillId == request.BillId
                                       select new BillPdfItemModel
                                       {
                                           ServiceName = service.Name,
                                           PetName = pet.Name,
                                           Price = invoice.TotalAmount
                                       }).ToListAsync(cancellationToken);
                
                // Add index to items
                var indexedBillItems = billItems.Select((item, index) => item with { Index = index + 1 }).ToList();

                // 3. Assemble the final data model for the PDF
                var pdfModel = new BillPdfModel
                {
                    BillCode = billAndCustomer.Bill.Code,
                    BillDate = billAndCustomer.Bill.BillDate,
                    CustomerName = billAndCustomer.Customer.FullName,
                    CustomerPhone = billAndCustomer.Customer.PhoneNumber,
                    CustomerAddress = billAndCustomer.Customer.Address ?? string.Empty,
                    Items = indexedBillItems,
                    TotalAmount = billAndCustomer.Bill.TotalAmount
                    // Clinic info is hardcoded in the model for now, can be moved to settings
                };

                // 4. Generate and return the PDF byte array
                return _pdfService.GenerateBillPdf(pdfModel);
            }
        }
    }
}
