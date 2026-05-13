using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryClinic.Business;
using VeterinaryClinic.Infrastructure;
using VeterinaryClinic.Shared; // <-- Sửa using ở đây

namespace VeterinaryClinic.Infrastructure
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // Đăng ký các lớp cấu hình từ Shared
            services.Configure<CloudinarySettings>(config.GetSection("CloudinarySettings"));
            services.Configure<MailSettings>(config.GetSection("EmailSettings"));
            services.Configure<VnPaySettings>(config.GetSection("VnPaySettings"));
            
            // Đăng ký các service của Infrastructure
            services.AddScoped<ICloudinaryService, PhotoService>();
            services.AddScoped<IEmailService, EmailService>();

            return services;
        }
    }
}
