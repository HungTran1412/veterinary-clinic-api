using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VeterinaryClinic.Data.Synchronization;

namespace VeterinaryClinic.Infrastructure.Synchronization;

public sealed class ReadModelSynchronizationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReadModelSynchronizationOptions> options,
    ILogger<ReadModelSynchronizationWorker> logger,
    IReadModelSynchronizationSignal? signal = null) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int processed;
                await using (var scope = scopeFactory.CreateAsyncScope())
                {
                    processed = await scope.ServiceProvider.GetRequiredService<ReadModelOutboxProcessor>().ProcessBatchAsync(stoppingToken);
                }
                if (processed > 0)
                    continue;

                var timeout = TimeSpan.FromSeconds(options.Value.PollIntervalSeconds);
                if (signal == null)
                    await Task.Delay(timeout, stoppingToken);
                else
                    await signal.WaitAsync(timeout, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Đồng bộ Read DB thất bại; các bản ghi Outbox chưa hoàn tất sẽ được thử lại.");
                try { await Task.Delay(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }
}
