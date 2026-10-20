using Microsoft.Extensions.Options;
using System.Runtime.ExceptionServices;
using VeterinaryClinic.Data.Synchronization;

namespace VeterinaryClinic.Infrastructure.Synchronization;

public sealed class ReadModelOutboxProcessor(
    IReadModelProjectionStore store,
    IOptions<ReadModelSynchronizationOptions> options)
{
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lease = await store.AcquireConsumerLockAsync(cancellationToken);
        if (lease == null)
            return 0;

        Exception? error = null;
        var processed = 0;
        try
        {
            var pending = await store.ReadPendingAsync(options.Value.BatchSize, cancellationToken);
            if (pending.Count > 0)
            {
                var states = new List<ReadModelProjectionState>(pending.Count);
                foreach (var message in pending)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    states.Add(await store.ReadCurrentStateAsync(message, cancellationToken));
                }
                await store.ApplyAsync(states, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await store.MarkProcessedAsync(pending, cancellationToken);
                processed = pending.Count;
            }
        }
        catch (Exception exception) { error = exception; }
        finally
        {
            try { await lease.DisposeAsync(); }
            catch (Exception exception) { error ??= exception; }
        }

        if (error != null)
            ExceptionDispatchInfo.Capture(error).Throw();
        return processed;
    }
}
