using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace VeterinaryClinic.Infrastructure.Synchronization;

public sealed class ReadModelProjectionSql(DbContext context)
{
    public IEntityType Validate(ReadModelProjectionState state)
    {
        var entityType = context.Model.FindEntityType(state.EntityType)
            ?? throw new InvalidOperationException("Loại dữ liệu Outbox không thuộc mô hình đọc.");
        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException("Dữ liệu đồng bộ phải có khóa chính.");
        if (state.KeyValues.Count != primaryKey.Properties.Count ||
            primaryKey.Properties.Any(p => !state.KeyValues.TryGetValue(p.Name, out var value) || !IsValidValue(p, value, false)))
            throw new InvalidOperationException("Khóa Outbox không khớp mô hình dữ liệu.");

        if (state.Values != null)
        {
            var properties = entityType.GetProperties().ToArray();
            if (state.Values.Count != properties.Length || properties.Any(p =>
                    !state.Values.TryGetValue(p.Name, out var value) || !IsValidValue(p, value, p.IsNullable)) ||
                primaryKey.Properties.Any(p => !Equals(state.KeyValues[p.Name], state.Values[p.Name])))
                throw new InvalidOperationException("Trạng thái nguồn không khớp mô hình dữ liệu.");
        }

        return entityType;
    }

    public DbCommand CreateExists(ReadModelProjectionState state)
    {
        var entityType = Validate(state);
        var command = CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {Table(entityType)} WHERE {Predicate(command, entityType, state.KeyValues)}";
        return command;
    }

    public DbCommand CreateDelete(ReadModelProjectionState state)
    {
        var entityType = Validate(state);
        var command = CreateCommand();
        command.CommandText = $"DELETE FROM {Table(entityType)} WHERE {Predicate(command, entityType, state.KeyValues)}";
        return command;
    }

    public DbCommand? CreateUpdate(ReadModelProjectionState state)
    {
        var entityType = Validate(state);
        var primaryKey = entityType.FindPrimaryKey()!;
        var properties = WritableProperties(entityType).Where(p => !primaryKey.Properties.Contains(p)).ToArray();
        if (properties.Length == 0)
            return null;

        var command = CreateCommand();
        var assignments = properties.Select(p => $"{Column(entityType, p)} = {Parameter(command, p, state.Values![p.Name])}");
        command.CommandText = $"UPDATE {Table(entityType)} SET {string.Join(", ", assignments)} WHERE {Predicate(command, entityType, state.KeyValues)}";
        return command;
    }

    public DbCommand CreateInsert(ReadModelProjectionState state)
    {
        var entityType = Validate(state);
        var command = CreateCommand();
        var properties = WritableProperties(entityType).ToArray();
        var values = properties.Select(p => Parameter(command, p, state.Values![p.Name]));
        command.CommandText = $"INSERT INTO {Table(entityType)} ({string.Join(", ", properties.Select(p => Column(entityType, p)))}) VALUES ({string.Join(", ", values)})";
        return command;
    }

    public DbCommand CreateIdentityInsert(IEntityType entityType, bool enabled)
    {
        if (!context.Database.IsSqlServer() || context.Model.FindEntityType(entityType.Name) != entityType)
            throw new InvalidOperationException("IDENTITY_INSERT chỉ được dùng với bảng SQL Server đã ánh xạ.");
        var command = CreateCommand();
        command.CommandText = $"SET IDENTITY_INSERT {Table(entityType)} {(enabled ? "ON" : "OFF")}";
        return command;
    }

    public bool HasIdentity(IEntityType entityType) => context.Database.IsSqlServer() &&
        entityType.GetProperties().Any(p => p.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn);

    private DbCommand CreateCommand()
    {
        var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandTimeout = context.Database.GetCommandTimeout() ?? 30;
        return command;
    }

    private string Table(IEntityType entityType) => context.GetService<ISqlGenerationHelper>().DelimitIdentifier(
        entityType.GetTableName() ?? throw new InvalidOperationException("Entity không ánh xạ tới bảng."), entityType.GetSchema());

    private string Column(IEntityType entityType, IProperty property)
    {
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        return context.GetService<ISqlGenerationHelper>().DelimitIdentifier(property.GetColumnName(table)
            ?? throw new InvalidOperationException("Thuộc tính không ánh xạ tới cột."));
    }

    private string Predicate(DbCommand command, IEntityType entityType, IReadOnlyDictionary<string, object?> keys) =>
        string.Join(" AND ", entityType.FindPrimaryKey()!.Properties.Select(p =>
            $"{Column(entityType, p)} = {Parameter(command, p, keys[p.Name])}"));

    private static string Parameter(DbCommand command, IProperty property, object? value)
    {
        var name = $"@p{command.Parameters.Count}";
        command.Parameters.Add(property.GetRelationalTypeMapping().CreateParameter(command, name, value, property.IsNullable));
        return name;
    }

    private static IEnumerable<IProperty> WritableProperties(IEntityType entityType) => entityType.GetProperties()
        .Where(p => p.ValueGenerated != ValueGenerated.OnAddOrUpdate && p.GetComputedColumnSql() == null);

    private static bool IsValidValue(IProperty property, object? value, bool allowNull) => value == null
        ? allowNull
        : (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsInstanceOfType(value);
}
