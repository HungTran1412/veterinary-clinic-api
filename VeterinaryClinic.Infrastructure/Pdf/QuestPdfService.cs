using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using VeterinaryClinic.Business;

namespace VeterinaryClinic.Infrastructure
{
    public class QuestPdfService : IPdfService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public QuestPdfService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        public byte[] GenerateBillPdf(BillPdfModel model)
        {
            var hydratedModel = model with
            {
                ClinicName = string.IsNullOrWhiteSpace(model.ClinicName)
                    ? _configuration["ClinicInfo:Name"] ?? "Phòng khám Thú y VetCare"
                    : model.ClinicName,
                ClinicAddress = string.IsNullOrWhiteSpace(model.ClinicAddress)
                    ? _configuration["ClinicInfo:Address"] ?? string.Empty
                    : model.ClinicAddress,
                ClinicPhone = string.IsNullOrWhiteSpace(model.ClinicPhone)
                    ? _configuration["ClinicInfo:PhoneNumber"] ?? string.Empty
                    : model.ClinicPhone,
                LogoImageBytes = model.LogoImageBytes ?? TryReadLogoBytes()
            };

            var document = new BillDocument(hydratedModel);
            return document.GeneratePdf();
        }

        private byte[]? TryReadLogoBytes()
        {
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                return null;
            }

            var logoPath = Path.Combine(webRootPath, "Image", "logo.png");
            return File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;
        }
    }
}
