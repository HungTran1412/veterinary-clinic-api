using Microsoft.Extensions.Options;
using Moq;
using VeterinaryClinic.Data;
using VeterinaryClinic.Data.Synchronization;
using VeterinaryClinic.Infrastructure.Synchronization;
using Xunit;

namespace VeterinaryClinic.Tests.Synchronization;

public sealed class ProjectionProcessorTests
{
    [Fact]
    public async Task ProcessBatch_HoldsLeaseUntilReplicaCommitAndSourceAcknowledgement()
    {
        // Kiểm tra giữ khóa xuyên đọc nguồn, commit replica và ack trước khi trả số bản ghi thành công.
        var calls = new List<string>();
        var lease = new TestLease(() => calls.Add("release"));
        var message = Message();
        var state = State();
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease).Callback(() => calls.Add("lock"));
        store.Setup(x => x.ReadPendingAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { message }).Callback(() => calls.Add("pending"));
        store.Setup(x => x.ReadCurrentStateAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(state).Callback(() => calls.Add("source"));
        store.Setup(x => x.ApplyAsync(It.Is<IReadOnlyList<ReadModelProjectionState>>(s => s.Count == 1 && s[0] == state), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask).Callback(() => calls.Add("commit"));
        store.Setup(x => x.MarkProcessedAsync(It.Is<IReadOnlyList<ReadModelOutboxMessage>>(m => m.Count == 1 && m[0] == message), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask).Callback(() => calls.Add("ack"));
        var processor = new ReadModelOutboxProcessor(store.Object, Options.Create(new ReadModelSynchronizationOptions()));

        var processed = await processor.ProcessBatchAsync();

        Assert.Equal(1, processed);
        Assert.Equal(new[] { "lock", "pending", "source", "commit", "ack", "release" }, calls);
    }

    [Fact]
    public async Task ProcessBatch_WhenLeaseBusy_DoesNotReadOrWrite()
    {
        // Kiểm tra consumer khác giữ khóa thì không đọc pending, không ghi replica hay ack.
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IAsyncDisposable?)null);
        var processor = Processor(store);

        Assert.Equal(0, await processor.ProcessBatchAsync());
        store.Verify(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessBatch_WhenNoPending_ReleasesLeaseWithoutApplying()
    {
        // Kiểm tra batch rỗng vẫn trả khóa và không mở transaction replica.
        var released = false;
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestLease(() => released = true));
        store.Setup(x => x.ReadPendingAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ReadModelOutboxMessage>());

        Assert.Equal(0, await Processor(store).ProcessBatchAsync());
        Assert.True(released);
    }

    [Fact]
    public async Task ProcessBatch_WhenReplicaFails_DoesNotAcknowledgeAndPreservesOriginalError()
    {
        // Kiểm tra lỗi ghi replica không ack và lỗi trả khóa không che lỗi nghiệp vụ gốc.
        var original = new IOException("replica unavailable");
        var released = false;
        var store = ReadyStore(new TestLease(() => { released = true; throw new IOException("release failed"); }));
        store.Setup(x => x.ApplyAsync(It.IsAny<IReadOnlyList<ReadModelProjectionState>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(original);

        var error = await Assert.ThrowsAsync<IOException>(() => Processor(store).ProcessBatchAsync());

        Assert.Same(original, error);
        Assert.True(released);
        store.Verify(x => x.MarkProcessedAsync(It.IsAny<IReadOnlyList<ReadModelOutboxMessage>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatch_WhenSourceMessageInvalid_DoesNotApplyAnyRows()
    {
        // Kiểm tra message lỗi giữ nguyên pending và dừng trước mọi thay đổi ở replica.
        var store = ReadyStore();
        store.Setup(x => x.ReadCurrentStateAsync(It.IsAny<ReadModelOutboxMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("invalid key"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Processor(store).ProcessBatchAsync());

        store.Verify(x => x.ApplyAsync(It.IsAny<IReadOnlyList<ReadModelProjectionState>>(), It.IsAny<CancellationToken>()), Times.Never);
        store.Verify(x => x.MarkProcessedAsync(It.IsAny<IReadOnlyList<ReadModelOutboxMessage>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatch_WhenCancelledAfterReplicaCommit_DoesNotAcknowledge()
    {
        // Kiểm tra cancellation sau commit replica để lại pending cho lần khởi động lại.
        using var cancellation = new CancellationTokenSource();
        var released = false;
        var store = ReadyStore(new TestLease(() => released = true));
        store.Setup(x => x.ApplyAsync(It.IsAny<IReadOnlyList<ReadModelProjectionState>>(), It.IsAny<CancellationToken>()))
            .Callback(cancellation.Cancel).Returns(Task.CompletedTask);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Processor(store).ProcessBatchAsync(cancellation.Token));

        Assert.True(released);
        store.Verify(x => x.MarkProcessedAsync(It.IsAny<IReadOnlyList<ReadModelOutboxMessage>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatch_WhenAcknowledgementFails_RereadsCurrentStateOnRetry()
    {
        // Kiểm tra lỗi ack sau commit được retry bằng trạng thái nguồn mới thay vì snapshot cũ.
        var store = ReadyStore();
        var first = State();
        var current = State() with { Values = null };
        var applied = new List<ReadModelProjectionState>();
        store.SetupSequence(x => x.ReadCurrentStateAsync(It.IsAny<ReadModelOutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(first).ReturnsAsync(current);
        store.Setup(x => x.ApplyAsync(It.IsAny<IReadOnlyList<ReadModelProjectionState>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<ReadModelProjectionState>, CancellationToken>((rows, _) => applied.Add(rows[0]))
            .Returns(Task.CompletedTask);
        store.SetupSequence(x => x.MarkProcessedAsync(It.IsAny<IReadOnlyList<ReadModelOutboxMessage>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("source ack unavailable")).Returns(Task.CompletedTask);
        var processor = Processor(store);

        await Assert.ThrowsAsync<IOException>(() => processor.ProcessBatchAsync());
        Assert.Equal(1, await processor.ProcessBatchAsync());

        Assert.Equal(new[] { first, current }, applied);
        Assert.Null(applied[1].Values);
    }

    private static ReadModelOutboxProcessor Processor(Mock<IReadModelProjectionStore> store) =>
        new(store.Object, Options.Create(new ReadModelSynchronizationOptions()));

    private static Mock<IReadModelProjectionStore> ReadyStore(IAsyncDisposable? lease = null)
    {
        var message = Message();
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => lease ?? new TestLease(() => { }));
        store.Setup(x => x.ReadPendingAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { message });
        store.Setup(x => x.ReadCurrentStateAsync(message, It.IsAny<CancellationToken>())).ReturnsAsync(State());
        return store;
    }

    private static ReadModelOutboxMessage Message() => new()
    {
        Id = 1, EntityType = typeof(VcDoctorSpecializations).FullName!,
        KeyValuesJson = "{\"DoctorId\":7,\"SpecializationId\":3}", CreatedAtUtc = DateTime.UtcNow
    };

    private static ReadModelProjectionState State() => new(typeof(VcDoctorSpecializations).FullName!,
        new Dictionary<string, object?> { ["DoctorId"] = 7, ["SpecializationId"] = 3 },
        new Dictionary<string, object?> { ["DoctorId"] = 7, ["SpecializationId"] = 3 });

    private sealed class TestLease(Action onDispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            onDispose();
            return ValueTask.CompletedTask;
        }
    }
}
