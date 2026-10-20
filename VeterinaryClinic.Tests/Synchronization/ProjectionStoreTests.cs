using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VeterinaryClinic.Data;
using VeterinaryClinic.Data.Synchronization;
using VeterinaryClinic.Infrastructure.Synchronization;
using Xunit;

namespace VeterinaryClinic.Tests.Synchronization;

public sealed class ProjectionStoreTests
{
    [Fact]
    public async Task Apply_InsertUpdateAndReplay_PreservesIdentityAuditAndCurrentState()
    {
        // Kiểm tra replica giữ ID/audit nguồn và replay cùng message lấy dữ liệu mới nhất, không nhân đôi.
        await using var database = await ProjectionDatabase.CreateAsync();
        var entity = Specialization(47, "Chuyên khoa cũ");
        database.Source.Add(entity);
        await database.Source.SaveChangesAsync();
        var auditCreatedAt = entity.CreatedDate;
        var message = Message<VcSpecializations>(new { Id = entity.Id });
        await database.Store.ApplyAsync(new[] { await database.Store.ReadCurrentStateAsync(message, default) }, default);
        await using (var read = database.ReadContext())
        {
            var row = await read.VcSpecializations.SingleAsync();
            Assert.Equal(47, row.Id);
            Assert.Equal(auditCreatedAt, row.CreatedDate);
            Assert.Equal(entity.CreatedUserName, row.CreatedUserName);
        }

        entity = await database.Source.VcSpecializations.SingleAsync();
        entity.Name = "Chuyên khoa mới";
        await database.Source.SaveChangesAsync();
        var latest = await database.Store.ReadCurrentStateAsync(message, default);
        await database.Store.ApplyAsync(new[] { latest, latest }, default);

        await using var finalRead = database.ReadContext();
        var current = await finalRead.VcSpecializations.SingleAsync();
        Assert.Equal("Chuyên khoa mới", current.Name);
        Assert.Equal(entity.ModifiedDate, current.ModifiedDate);
        Assert.Equal(auditCreatedAt, current.CreatedDate);
    }

    [Fact]
    public async Task Apply_CompositeKeyAndHardDelete_OnlyChangesMatchingRow()
    {
        // Kiểm tra cả hai thành phần khóa ghép được giữ và xóa đúng bản ghi nguồn đã bị xóa cứng.
        await using var database = await ProjectionDatabase.CreateAsync();
        database.Source.AddRange(new VcDoctorSpecializations { DoctorId = 7, SpecializationId = 3 },
            new VcDoctorSpecializations { DoctorId = 7, SpecializationId = 4 });
        await database.Source.SaveChangesAsync();
        var first = Message<VcDoctorSpecializations>(new { DoctorId = 7, SpecializationId = 3 });
        var second = Message<VcDoctorSpecializations>(new { SpecializationId = 4, DoctorId = 7 });
        await database.Store.ApplyAsync(new[]
        {
            await database.Store.ReadCurrentStateAsync(first, default),
            await database.Store.ReadCurrentStateAsync(second, default)
        }, default);
        database.Source.Remove(await database.Source.VcDoctorSpecializations.SingleAsync(x => x.SpecializationId == 3));
        await database.Source.SaveChangesAsync();
        var deleted = await database.Store.ReadCurrentStateAsync(first, default);

        await database.Store.ApplyAsync(new[] { deleted, deleted }, default);

        Assert.Null(deleted.Values);
        await using var read = database.ReadContext();
        var survivor = await read.VcDoctorSpecializations.SingleAsync();
        Assert.Equal(7, survivor.DoctorId);
        Assert.Equal(4, survivor.SpecializationId);
    }

    [Fact]
    public async Task Apply_WhenLaterRowFails_RollsBackEarlierRowAndKeepsOutboxPending()
    {
        // Kiểm tra lỗi giữa batch rollback cả bản ghi đã ghi trước đó và không hoàn tất Outbox.
        await using var database = await ProjectionDatabase.CreateAsync();
        database.Source.AddRange(Specialization(1, "Được phép"), Specialization(2, "reject"));
        var first = Message<VcSpecializations>(new { Id = 1 });
        var second = Message<VcSpecializations>(new { Id = 2 });
        database.Source.ReadModelOutboxMessages.AddRange(first, second);
        await database.Source.SaveChangesAsync();
        await using (var read = database.ReadContext())
        {
            await read.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER reject_projection BEFORE INSERT ON "vcSpecializations"
                WHEN NEW."name" = 'reject'
                BEGIN SELECT RAISE(ABORT, 'projection rejected'); END;
                """);
        }
        var states = new[]
        {
            await database.Store.ReadCurrentStateAsync(first, default),
            await database.Store.ReadCurrentStateAsync(second, default)
        };

        await Assert.ThrowsAsync<SqliteException>(() => database.Store.ApplyAsync(states, default));

        await using var finalRead = database.ReadContext();
        Assert.Empty(await finalRead.VcSpecializations.ToListAsync());
        Assert.Equal(2, await database.Source.ReadModelOutboxMessages.CountAsync(x => x.ProcessedAtUtc == null));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"Id\":null}")]
    [InlineData("{\"Id\":\"1\"}")]
    [InlineData("{\"Id\":1,\"Other\":2}")]
    [InlineData("{\"Id\":1,\"Id\":2}")]
    [InlineData("[]")]
    [InlineData("invalid json")]
    public async Task ReadCurrentState_WhenKeyMalformed_RejectsBeforeReplicaWrite(string json)
    {
        // Kiểm tra khóa thiếu, null, sai kiểu, thừa, trùng hoặc JSON lỗi không được dùng ghi replica.
        await using var database = await ProjectionDatabase.CreateAsync();
        var message = Message<VcSpecializations>(new { Id = 1 });
        message.KeyValuesJson = json;

        await Assert.ThrowsAsync<InvalidOperationException>(() => database.Store.ReadCurrentStateAsync(message, default));

        await using var read = database.ReadContext();
        Assert.Empty(await read.VcSpecializations.ToListAsync());
        Assert.Null(message.ProcessedAtUtc);
    }

    [Theory]
    [InlineData("VeterinaryClinic.Data.Synchronization.ReadModelOutboxMessage")]
    [InlineData("vcUsers]; DROP TABLE vcUsers; --")]
    public async Task ReadCurrentState_WhenEntityOutsideReadModel_RejectsUntrustedIdentifier(string entityType)
    {
        // Kiểm tra tên entity lạ hoặc chính Outbox không thể trở thành tên bảng SQL động.
        await using var database = await ProjectionDatabase.CreateAsync();
        var message = Message<VcSpecializations>(new { Id = 1 });
        message.EntityType = entityType;

        await Assert.ThrowsAsync<InvalidOperationException>(() => database.Store.ReadCurrentStateAsync(message, default));

        await using var read = database.ReadContext();
        Assert.Empty(await read.VcSpecializations.ToListAsync());
    }

    [Fact]
    public async Task ReadPending_WhenLowerIdentityCommitsLater_DoesNotSkipIt()
    {
        // Kiểm tra pending ID thấp xuất hiện sau khi ID cao đã ack vẫn được đọc ở lượt sau.
        await using var database = await ProjectionDatabase.CreateAsync();
        var high = Message<VcSpecializations>(new { Id = 20 });
        high.Id = 20;
        database.Source.ReadModelOutboxMessages.Add(high);
        await database.Source.SaveChangesAsync();
        var first = await database.Store.ReadPendingAsync(1, default);
        Assert.Equal(20, Assert.Single(first).Id);
        await database.Store.MarkProcessedAsync(first, default);
        var low = Message<VcSpecializations>(new { Id = 10 });
        low.Id = 10;
        database.Source.ReadModelOutboxMessages.Add(low);
        await database.Source.SaveChangesAsync();

        var pending = await database.Store.ReadPendingAsync(1, default);

        Assert.Equal(10, Assert.Single(pending).Id);
        Assert.NotNull(high.ProcessedAtUtc);
        Assert.Null(low.ProcessedAtUtc);
    }

    [Fact]
    public async Task MarkProcessed_WithCaptureEnabled_DoesNotCreateOutboxFeedback()
    {
        // Kiểm tra luồng capture → projection → ack không sinh thêm Outbox hay sửa audit nghiệp vụ.
        await using var database = await ProjectionDatabase.CreateAsync(captureEnabled: true);
        var entity = Specialization(17, "Tim mạch");
        database.Source.Add(entity);
        await database.Source.SaveChangesAsync();
        var modifiedAt = entity.ModifiedDate;
        var pending = await database.Store.ReadPendingAsync(100, default);
        var message = Assert.Single(pending);
        await database.Store.ApplyAsync(new[] { await database.Store.ReadCurrentStateAsync(message, default) }, default);

        await database.Store.MarkProcessedAsync(pending, default);

        Assert.Equal(1, await database.Source.ReadModelOutboxMessages.CountAsync());
        Assert.Empty(await database.Store.ReadPendingAsync(100, default));
        Assert.NotNull(message.ProcessedAtUtc);
        await using var read = database.ReadContext();
        Assert.Equal(modifiedAt, (await read.VcSpecializations.SingleAsync()).ModifiedDate);
    }

    [Fact]
    public async Task Apply_WhenCancelled_DoesNotChangeReplica()
    {
        // Kiểm tra token đã hủy không được tạo dữ liệu replica hoặc đánh dấu hoàn tất message.
        await using var database = await ProjectionDatabase.CreateAsync();
        database.Source.Add(Specialization(1, "Chuyên khoa"));
        await database.Source.SaveChangesAsync();
        var state = await database.Store.ReadCurrentStateAsync(Message<VcSpecializations>(new { Id = 1 }), default);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => database.Store.ApplyAsync(new[] { state }, cancellation.Token));

        await using var read = database.ReadContext();
        Assert.Empty(await read.VcSpecializations.ToListAsync());
    }

    private static VcSpecializations Specialization(int id, string name) => new()
    {
        Id = id, Code = $"SP{id}", Name = name, CreatedUserName = "Người tạo", ModifiedUserName = "Người sửa"
    };

    private static ReadModelOutboxMessage Message<T>(object key) => new()
    {
        EntityType = typeof(T).FullName!, KeyValuesJson = JsonSerializer.Serialize(key), CreatedAtUtc = DateTime.UtcNow
    };

    private sealed class ProjectionDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _sourceConnection;
        private readonly SqliteConnection _replicaConnection;
        private readonly DbContextOptions<VeterinaryClinicReadDataContext> _readOptions;
        public VeterinaryClinicDataContext Source { get; }
        public RelationalReadModelProjectionStore Store { get; }

        private ProjectionDatabase(SqliteConnection source, SqliteConnection replica, bool captureEnabled)
        {
            _sourceConnection = source;
            _replicaConnection = replica;
            Source = new VeterinaryClinicDataContext(new DbContextOptionsBuilder<VeterinaryClinicDataContext>()
                .UseSqlite(source).Options, () => null!, Options.Create(new ReadModelSynchronizationOptions { Enabled = captureEnabled }));
            _readOptions = new DbContextOptionsBuilder<VeterinaryClinicReadDataContext>().UseSqlite(replica).Options;
            Store = new RelationalReadModelProjectionStore(Source, _readOptions);
        }

        public static async Task<ProjectionDatabase> CreateAsync(bool captureEnabled = false)
        {
            var source = new SqliteConnection("Data Source=:memory:");
            var replica = new SqliteConnection("Data Source=:memory:");
            await source.OpenAsync();
            await replica.OpenAsync();
            var database = new ProjectionDatabase(source, replica, captureEnabled);
            await database.Source.Database.EnsureCreatedAsync();
            await using var read = database.ReadContext();
            await read.Database.EnsureCreatedAsync();
            return database;
        }

        public VeterinaryClinicReadDataContext ReadContext() => new(_readOptions);

        public async ValueTask DisposeAsync()
        {
            await Store.DisposeAsync();
            await Source.DisposeAsync();
            await _sourceConnection.DisposeAsync();
            await _replicaConnection.DisposeAsync();
        }
    }
}
