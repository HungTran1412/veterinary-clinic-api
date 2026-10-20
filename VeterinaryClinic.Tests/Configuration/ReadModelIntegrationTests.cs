using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VeterinaryClinic.Data;
using VeterinaryClinic.Data.Synchronization;
using VeterinaryClinic.Infrastructure.Synchronization;
using Xunit;

namespace VeterinaryClinic.Tests.Configuration;

public class ReadModelIntegrationTests
{
    // Kiểm tra xuyên suốt capture, projection và ack: thêm/sửa/xóa hội tụ đúng ID, không tự phát Outbox khi ack.
    [Fact]
    public async Task CaptureAndProjection_SynchronizeInsertUpdateDeleteAndAcknowledgeWithoutFeedback()
    {
        // Arrange
        await using var writeConnection = new SqliteConnection("Data Source=:memory:");
        await using var readConnection = new SqliteConnection("Data Source=:memory:");
        await writeConnection.OpenAsync();
        await readConnection.OpenAsync();
        var synchronization = Options.Create(new ReadModelSynchronizationOptions { Enabled = true });
        var writeOptions = new DbContextOptionsBuilder<VeterinaryClinicDataContext>().UseSqlite(writeConnection).Options;
        var readOptions = new DbContextOptionsBuilder<VeterinaryClinicReadDataContext>().UseSqlite(readConnection).Options;
        await using var source = new VeterinaryClinicDataContext(writeOptions, null!, synchronization);
        await using var replica = new VeterinaryClinicReadDataContext(readOptions);
        await source.Database.EnsureCreatedAsync();
        await replica.Database.EnsureCreatedAsync();
        await using var store = new RelationalReadModelProjectionStore(source, readOptions);
        var processor = new ReadModelOutboxProcessor(new IsolatedStore(store), synchronization);
        var entity = new VcSpecializations { Code = "SP01", Name = "Khám tổng quát" };

        // Act: thêm và xác nhận replica có cùng identity và audit.
        source.VcSpecializations.Add(entity);
        await source.SaveChangesAsync();
        Assert.Equal(1, await processor.ProcessBatchAsync());
        var projected = await replica.VcSpecializations.AsNoTracking().SingleAsync();
        Assert.Equal(entity.Id, projected.Id);
        Assert.Equal(entity.Name, projected.Name);
        Assert.Equal(entity.CreatedDate, projected.CreatedDate);

        // Act: cập nhật bản ghi đã có, sau đó xóa cứng tại nguồn.
        entity.Name = "Da liễu";
        source.Update(entity);
        await source.SaveChangesAsync();
        Assert.Equal(1, await processor.ProcessBatchAsync());
        Assert.Equal("Da liễu", await replica.VcSpecializations.AsNoTracking().Select(x => x.Name).SingleAsync());
        source.Remove(entity);
        await source.SaveChangesAsync();
        Assert.Equal(1, await processor.ProcessBatchAsync());

        // Assert
        Assert.Empty(await replica.VcSpecializations.AsNoTracking().ToListAsync());
        Assert.Equal(3, await source.ReadModelOutboxMessages.CountAsync());
        Assert.Equal(0, await source.ReadModelOutboxMessages.CountAsync(x => x.ProcessedAtUtc == null));
        Assert.Equal(0, await processor.ProcessBatchAsync());
    }

    // SQLite không có sp_getapplock; chỉ thay lease, các thao tác dữ liệu dùng store thật.
    private sealed class IsolatedStore(IReadModelProjectionStore inner) : IReadModelProjectionStore
    {
        public Task<IAsyncDisposable?> AcquireConsumerLockAsync(CancellationToken cancellationToken)
            => Task.FromResult<IAsyncDisposable?>(new TestLease());
        public Task<IReadOnlyList<ReadModelOutboxMessage>> ReadPendingAsync(int batchSize, CancellationToken cancellationToken)
            => inner.ReadPendingAsync(batchSize, cancellationToken);
        public Task<ReadModelProjectionState> ReadCurrentStateAsync(ReadModelOutboxMessage message, CancellationToken cancellationToken)
            => inner.ReadCurrentStateAsync(message, cancellationToken);
        public Task ApplyAsync(IReadOnlyList<ReadModelProjectionState> states, CancellationToken cancellationToken)
            => inner.ApplyAsync(states, cancellationToken);
        public Task MarkProcessedAsync(IReadOnlyList<ReadModelOutboxMessage> messages, CancellationToken cancellationToken)
            => inner.MarkProcessedAsync(messages, cancellationToken);
    }

    private sealed class TestLease : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
