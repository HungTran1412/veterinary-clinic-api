using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryClinic.Business;
using VeterinaryClinic.Infrastructure.Services;

namespace VeterinaryClinic.Infrastructure.Configurations
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<CloudinarySettings>(config.GetSection("CloudinarySettings"));
            services.AddScoped<ICloudinaryService, PhotoService>();

            return services;
        }
    }
}