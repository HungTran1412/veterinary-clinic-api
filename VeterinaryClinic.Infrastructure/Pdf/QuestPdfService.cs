using QuestPDF.Fluent;
using VeterinaryClinic.Business;

namespace VeterinaryClinic.Infrastructure
{
    public class QuestPdfService : IPdfService
    {
        public byte[] GenerateBillPdf(BillPdfModel model)
        {
            var document = new BillDocument(model);
            return document.GeneratePdf();
        }
    }
}
