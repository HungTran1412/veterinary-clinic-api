using Microsoft.Extensions.DependencyInjection;

namespace VeterinaryClinic.Infrastructure.Synchronization;

public static class ReadModelSynchronizationServiceCollectionExtensions
{
    public static IServiceCollection AddReadModelSynchronization(this IServiceCollection services)
    {
        services.AddScoped<IReadModelProjectionStore, RelationalReadModelProjectionStore>();
        services.AddScoped<ReadModelOutboxProcessor>();
        services.AddHostedService<ReadModelSynchronizationWorker>();
        return services;
    }
}
