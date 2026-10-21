using Microsoft.Extensions.DependencyInjection;
using VeterinaryClinic.Data.Synchronization;

namespace VeterinaryClinic.Infrastructure.Synchronization;

public static class ReadModelSynchronizationServiceCollectionExtensions
{
    public static IServiceCollection AddReadModelSynchronization(this IServiceCollection services)
    {
        services.AddSingleton<IReadModelSynchronizationSignal, ReadModelSynchronizationSignal>();
        services.AddScoped<IReadModelProjectionStore, RelationalReadModelProjectionStore>();
        services.AddScoped<ReadModelOutboxProcessor>();
        services.AddHostedService<ReadModelSynchronizationWorker>();
        return services;
    }
}
