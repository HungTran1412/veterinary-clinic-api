using System.Data;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using VeterinaryClinic.Data;
using VeterinaryClinic.Data.Synchronization;

namespace VeterinaryClinic.Infrastructure.Synchronization;

public sealed class RelationalReadModelProjectionStore : IReadModelProjectionStore, IDisposable, IAsyncDisposable
{
    private readonly VeterinaryClinicDataContext _source;
    private readonly VeterinaryClinicReadDataContext _replica;
    private readonly ReadModelProjectionSql _sql;

    public RelationalReadModelProjectionStore(VeterinaryClinicDataContext source,
        DbContextOptions<VeterinaryClinicReadDataContext> replicaOptions)
    {
        _source = source;
        _replica = new VeterinaryClinicReadDataContext(replicaOptions);
        _sql = new ReadModelProjectionSql(_replica);
    }

    public async Task<IAsyncDisposable?> AcquireConsumerLockAsync(CancellationToken cancellationToken)
    {
        if (!_source.Database.IsSqlServer())
            throw new InvalidOperationException("Khóa đồng bộ yêu cầu Write DB SQL Server.");
        await _source.Database.OpenConnectionAsync(cancellationToken);
        return await SqlServerReadModelConsumerLock.AcquireAsync(_source.Database.GetDbConnection(),
            () => _source.Database.CloseConnectionAsync(), cancellationToken);
    }

    public async Task<IReadOnlyList<ReadModelOutboxMessage>> ReadPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        return await _source.ReadModelOutboxMessages.AsTracking().Where(m => m.ProcessedAtUtc == null)
            .OrderBy(m => m.Id).Take(batchSize).ToListAsync(cancellationToken);
    }

    public async Task<ReadModelProjectionState> ReadCurrentStateAsync(ReadModelOutboxMessage message, CancellationToken cancellationToken)
    {
        var entityType = _replica.Model.FindEntityType(message.EntityType)
            ?? throw new InvalidOperationException("Loại dữ liệu Outbox không thuộc mô hình đọc.");
        var key = entityType.FindPrimaryKey()!;
        IReadOnlyDictionary<string, object?> keys;
        try
        {
            using var document = JsonDocument.Parse(message.KeyValuesJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonException();
            var properties = document.RootElement.EnumerateObject().ToArray();
            if (properties.Length != key.Properties.Count || properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length)
                throw new JsonException();
            keys = key.Properties.ToDictionary(p => p.Name, p => document.RootElement.GetProperty(p.Name).Deserialize(p.ClrType));
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new InvalidOperationException("Khóa Outbox không hợp lệ.", exception);
        }

        var state = new ReadModelProjectionState(entityType.Name, keys, null);
        _sql.Validate(state);
        var sourceType = _source.Model.FindEntityType(entityType.Name)
            ?? throw new InvalidOperationException("Loại dữ liệu Outbox không thuộc mô hình ghi.");
        var keyValues = key.Properties.Select(p => keys[p.Name]).ToArray();
        var tracked = _source.ChangeTracker.Entries().FirstOrDefault(e => e.Metadata == sourceType &&
            key.Properties.All(p => Equals(e.Property(p.Name).CurrentValue, keys[p.Name])));
        if (tracked != null)
        {
            if (tracked.State != EntityState.Unchanged)
                throw new InvalidOperationException("Scope đồng bộ không được chứa thay đổi nghiệp vụ chưa lưu.");
            tracked.State = EntityState.Detached;
        }

        var entity = await _source.FindAsync(sourceType.ClrType, keyValues, cancellationToken);
        if (entity == null)
            return state;

        var entry = _source.Entry(entity);
        try
        {
            return state with { Values = entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue) };
        }
        finally { entry.State = EntityState.Detached; }
    }

    public async Task ApplyAsync(IReadOnlyList<ReadModelProjectionState> states, CancellationToken cancellationToken)
    {
        if (!_replica.Database.IsSqlServer() && _replica.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
            throw new InvalidOperationException("Provider Read DB không hỗ trợ đồng bộ.");
        foreach (var state in states)
            _sql.Validate(state);
        if (states.Count == 0)
            return;

        var connection = _replica.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        IDbContextTransaction? transaction = null;
        IEntityType? identityEnabled = null;
        Exception? error = null;
        var cleanupFailed = false;
        try
        {
            if (!wasOpen)
                await _replica.Database.OpenConnectionAsync(cancellationToken);
            transaction = await _replica.Database.BeginTransactionAsync(cancellationToken);
            foreach (var state in states)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (state.Values == null)
                {
                    await using var delete = _sql.CreateDelete(state);
                    await delete.ExecuteNonQueryAsync(cancellationToken);
                    continue;
                }

                await using var exists = _sql.CreateExists(state);
                var found = Convert.ToInt64(await exists.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
                if (found)
                {
                    await using var update = _sql.CreateUpdate(state);
                    if (update != null)
                        await update.ExecuteNonQueryAsync(cancellationToken);
                    continue;
                }

                var entityType = _sql.Validate(state);
                if (_sql.HasIdentity(entityType))
                {
                    identityEnabled = entityType;
                    await using var enableIdentity = _sql.CreateIdentityInsert(entityType, true);
                    await enableIdentity.ExecuteNonQueryAsync(cancellationToken);
                }
                await using var insert = _sql.CreateInsert(state);
                await insert.ExecuteNonQueryAsync(cancellationToken);
                if (identityEnabled != null)
                {
                    await using var disableIdentity = _sql.CreateIdentityInsert(identityEnabled, false);
                    await disableIdentity.ExecuteNonQueryAsync(CancellationToken.None);
                    identityEnabled = null;
                }
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            error = exception;
            if (transaction != null)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch { cleanupFailed = true; }
            }
        }
        finally
        {
            if (transaction != null)
            {
                try { await transaction.DisposeAsync(); }
                catch (Exception exception) { error ??= exception; cleanupFailed = true; }
            }
            if (identityEnabled != null)
            {
                // Retry OFF ngoài transaction đã rollback, kể cả khi token caller đã bị hủy.
                try
                {
                    await using var disableIdentity = _sql.CreateIdentityInsert(identityEnabled, false);
                    await disableIdentity.ExecuteNonQueryAsync(CancellationToken.None);
                }
                catch (Exception exception) { error ??= exception; cleanupFailed = true; }
            }
            if (cleanupFailed)
            {
                try { SqlServerReadModelConsumerLock.ClearFailedSessionPool(connection); } catch { }
            }
            if (!wasOpen)
            {
                try { await _replica.Database.CloseConnectionAsync(); }
                catch (Exception exception) { error ??= exception; cleanupFailed = true; }
            }
            if (cleanupFailed)
            {
                try { SqlServerReadModelConsumerLock.ClearFailedSessionPool(connection); } catch { }
                try { await connection.CloseAsync(); } catch { }
            }
        }
        if (error != null)
            ExceptionDispatchInfo.Capture(error).Throw();
    }

    public async Task MarkProcessedAsync(IReadOnlyList<ReadModelOutboxMessage> messages, CancellationToken cancellationToken)
    {
        if (_source.ChangeTracker.Entries().Any(e => e.Entity is not ReadModelOutboxMessage &&
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Scope đồng bộ không được lưu thay đổi nghiệp vụ.");
        var processedAt = DateTime.UtcNow;
        foreach (var message in messages)
        {
            message.ProcessedAtUtc = processedAt;
            _source.Entry(message).Property(m => m.ProcessedAtUtc).IsModified = true;
        }
        await _source.SaveChangesAsync(cancellationToken);
    }

    public void Dispose() => _replica.Dispose();
    public ValueTask DisposeAsync() => _replica.DisposeAsync();
}
