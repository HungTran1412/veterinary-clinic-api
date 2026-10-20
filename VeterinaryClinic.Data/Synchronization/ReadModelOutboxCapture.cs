using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace VeterinaryClinic.Data.Synchronization;

internal static class ReadModelOutboxCapture
{
    public static int SaveChanges(DbContext context, bool accept, Func<bool, int> persist)
    {
        var entries = ChangedBusinessEntries(context);
        if (entries.Length == 0)
            return persist(accept);

        ValidateTransaction(context);
        var snapshots = SnapshotChanges(context);
        var external = context.Database.CurrentTransaction;
        using var owned = external == null ? context.Database.BeginTransaction() : null;
        var transaction = external ?? owned!;
        var savepoint = external == null ? null : "Outbox_" + Guid.NewGuid().ToString("N")[..24];
        if (savepoint != null)
            transaction.CreateSavepoint(savepoint);

        try
        {
            var count = persist(false);
            foreach (var entry in entries)
            {
                using var command = CreateInsertCommand(context, transaction, entry);
                command.ExecuteNonQuery();
            }
            if (owned != null)
                owned.Commit();
            else
                transaction.ReleaseSavepoint(savepoint!);

            if (accept)
                context.ChangeTracker.AcceptAllChanges();
            return count;
        }
        catch (Exception exception)
        {
            Recover(exception, snapshots, () =>
            {
                if (owned != null) owned.Rollback();
                else transaction.RollbackToSavepoint(savepoint!);
            });
            throw;
        }
    }

    public static async Task<int> SaveChangesAsync(DbContext context, bool accept,
        Func<bool, CancellationToken, Task<int>> persist, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entries = ChangedBusinessEntries(context);
        if (entries.Length == 0)
            return await persist(accept, cancellationToken);

        ValidateTransaction(context);
        var snapshots = SnapshotChanges(context);
        var external = context.Database.CurrentTransaction;
        await using var owned = external == null
            ? await context.Database.BeginTransactionAsync(cancellationToken) : null;
        var transaction = external ?? owned!;
        var savepoint = external == null ? null : "Outbox_" + Guid.NewGuid().ToString("N")[..24];
        if (savepoint != null)
            await transaction.CreateSavepointAsync(savepoint, cancellationToken);

        try
        {
            var count = await persist(false, cancellationToken);
            foreach (var entry in entries)
            {
                await using var command = CreateInsertCommand(context, transaction, entry);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            if (owned != null)
                await owned.CommitAsync(cancellationToken);
            else
                await transaction.ReleaseSavepointAsync(savepoint!, cancellationToken);

            if (accept)
                context.ChangeTracker.AcceptAllChanges();
            return count;
        }
        catch (Exception exception)
        {
            Exception? rollbackError = null;
            try
            {
                if (owned != null) await owned.RollbackAsync(CancellationToken.None);
                else await transaction.RollbackToSavepointAsync(savepoint!, CancellationToken.None);
            }
            catch (Exception error) { rollbackError = error; }
            Recover(exception, snapshots, () =>
            {
                if (rollbackError != null) throw rollbackError;
            });
            throw;
        }
    }

    private static EntityEntry[] ChangedBusinessEntries(DbContext context)
    {
        context.ChangeTracker.DetectChanges();
        return context.ChangeTracker.Entries().Where(entry => entry.Entity is not ReadModelOutboxMessage
            && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
    }

    private static OutboxEntrySnapshot[] SnapshotChanges(DbContext context) => context.ChangeTracker.Entries()
        .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
        .Select(entry => new OutboxEntrySnapshot(entry)).ToArray();

    private static void ValidateTransaction(DbContext context)
    {
        if (Transaction.Current != null)
            throw new InvalidOperationException("Đồng bộ Outbox không hỗ trợ giao dịch ngầm định. Hãy dùng transaction của DbContext.");
        if (!context.Database.IsRelational())
            throw new InvalidOperationException("Đồng bộ Outbox yêu cầu cơ sở dữ liệu quan hệ.");
        if (context.Database.CurrentTransaction is { SupportsSavepoints: false })
            throw new InvalidOperationException("Đồng bộ Outbox yêu cầu transaction hỗ trợ savepoint. Hãy tắt SQL Server MARS.");
    }

    private static DbCommand CreateInsertCommand(DbContext context, IDbContextTransaction transaction, EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Không thể ghi Outbox cho entity không có khóa chính: {entry.Metadata.Name}.");
        var values = key.Properties.ToDictionary(property => property.Name,
            property => entry.Property(property.Name).CurrentValue
                ?? throw new InvalidOperationException($"Khóa chính {property.Name} chưa có giá trị."));
        var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandTimeout = context.Database.GetCommandTimeout() ?? command.CommandTimeout;
        command.CommandText = "INSERT INTO [vcReadModelOutbox] ([EntityType], [KeyValuesJson], [CreatedAtUtc]) VALUES (@entityType, @keys, @createdAtUtc)";
        AddParameter(command, "@entityType", entry.Metadata.Name, DbType.String);
        AddParameter(command, "@keys", JsonSerializer.Serialize(values), DbType.String);
        AddParameter(command, "@createdAtUtc", DateTime.UtcNow, DbType.DateTime2);
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object value, DbType type)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        parameter.DbType = type;
        command.Parameters.Add(parameter);
    }

    private static void Recover(Exception original, OutboxEntrySnapshot[] snapshots, Action rollback)
    {
        var errors = new List<Exception> { original };
        try { rollback(); }
        catch (Exception error) { errors.Add(error); }
        foreach (var snapshot in snapshots)
        {
            try { snapshot.Restore(); }
            catch (Exception error) { errors.Add(error); }
        }
        if (errors.Count > 1)
            throw new AggregateException("Lưu Outbox thất bại và chưa khôi phục hoàn toàn. Không sử dụng lại DbContext này.", errors);
    }
}
