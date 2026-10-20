using VeterinaryClinic.Data.Synchronization;

namespace VeterinaryClinic.Infrastructure.Synchronization;

public sealed record ReadModelProjectionState(
    string EntityType,
    IReadOnlyDictionary<string, object?> KeyValues,
    IReadOnlyDictionary<string, object?>? Values);

public interface IReadModelProjectionStore
{
    Task<IAsyncDisposable?> AcquireConsumerLockAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ReadModelOutboxMessage>> ReadPendingAsync(int batchSize, CancellationToken cancellationToken);
    Task<ReadModelProjectionState> ReadCurrentStateAsync(ReadModelOutboxMessage message, CancellationToken cancellationToken);
    Task ApplyAsync(IReadOnlyList<ReadModelProjectionState> states, CancellationToken cancellationToken);
    Task MarkProcessedAsync(IReadOnlyList<ReadModelOutboxMessage> messages, CancellationToken cancellationToken);
}
