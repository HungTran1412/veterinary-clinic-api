using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VeterinaryClinic.Data.Synchronization;

namespace VeterinaryClinic.Infrastructure.Synchronization;

public sealed class ReadModelSynchronizationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReadModelSynchronizationOptions> options,
    ILogger<ReadModelSynchronizationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ReadModelOutboxProcessor>().ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Đồng bộ Read DB thất bại; các bản ghi Outbox chưa hoàn tất sẽ được thử lại.");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
