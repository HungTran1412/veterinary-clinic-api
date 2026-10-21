using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using VeterinaryClinic.Data.Synchronization;
using VeterinaryClinic.Infrastructure.Synchronization;
using Xunit;

namespace VeterinaryClinic.Tests.Synchronization;

public sealed class ProjectionWorkerTests
{
    [Fact]
    public async Task DisabledWorker_DoesNotCreateDatabaseScope()
    {
        // Kiểm tra worker mặc định tắt không tạo scope hay resolve context có thể mở database.
        var scopes = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        using var worker = new ReadModelSynchronizationWorker(scopes.Object,
            Options.Create(new ReadModelSynchronizationOptions { Enabled = false }),
            NullLogger<ReadModelSynchronizationWorker>.Instance);

        await worker.StartAsync(default);
        await worker.StopAsync(default);

        scopes.Verify(x => x.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task Worker_WhenPendingRemains_DrainsPartialBatchesWithoutPollingDelay()
    {
        // Kiểm tra hai batch nhỏ vẫn được xử lý liên tiếp đến rỗng mà không đợi chu kỳ polling.
        var options = Options.Create(new ReadModelSynchronizationOptions { Enabled = true, PollIntervalSeconds = 3600 });
        var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = new ReadModelOutboxMessage { Id = 1 };
        var second = new ReadModelOutboxMessage { Id = 2 };
        var acknowledged = new List<long>();
        var reads = 0;
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new WorkerLease(() => { }));
        store.Setup(x => x.ReadPendingAsync(100, It.IsAny<CancellationToken>())).Returns(() =>
        {
            var read = Interlocked.Increment(ref reads);
            if (read == 1)
                return Task.FromResult<IReadOnlyList<ReadModelOutboxMessage>>(new[] { first });
            if (read == 2)
                return Task.FromResult<IReadOnlyList<ReadModelOutboxMessage>>(new[] { second });
            drained.TrySetResult();
            return Task.FromResult<IReadOnlyList<ReadModelOutboxMessage>>(Array.Empty<ReadModelOutboxMessage>());
        });
        store.Setup(x => x.ReadCurrentStateAsync(It.IsAny<ReadModelOutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadModelProjectionState("test", new Dictionary<string, object?>(), null));
        store.Setup(x => x.ApplyAsync(It.IsAny<IReadOnlyList<ReadModelProjectionState>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        store.Setup(x => x.MarkProcessedAsync(It.IsAny<IReadOnlyList<ReadModelOutboxMessage>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<ReadModelOutboxMessage>, CancellationToken>((messages, _) => acknowledged.Add(messages[0].Id))
            .Returns(Task.CompletedTask);
        await using var provider = new ServiceCollection().AddSingleton<IOptions<ReadModelSynchronizationOptions>>(options)
            .AddSingleton(store.Object).AddScoped<ReadModelOutboxProcessor>().BuildServiceProvider();
        using var worker = new ReadModelSynchronizationWorker(provider.GetRequiredService<IServiceScopeFactory>(), options,
            NullLogger<ReadModelSynchronizationWorker>.Instance);

        try
        {
            await worker.StartAsync(default);
            await drained.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { await worker.StopAsync(default); }

        Assert.Equal(new long[] { 1, 2 }, acknowledged);
        Assert.Equal(3, reads);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Worker_WhenIdle_IsWokenByNotification()
    {
        // Kiểm tra worker đang rỗng nhận tín hiệu thì đồng bộ dữ liệu mới ngay cả khi polling rất dài.
        var options = Options.Create(new ReadModelSynchronizationOptions { Enabled = true, PollIntervalSeconds = 3600 });
        var signal = new ReadModelSynchronizationSignal();
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var projected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = 0;
        var message = new ReadModelOutboxMessage { Id = 7 };
        var state = new ReadModelProjectionState("test", new Dictionary<string, object?> { ["Id"] = 7 }, null);
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new WorkerLease(() => { }));
        store.Setup(x => x.ReadPendingAsync(100, It.IsAny<CancellationToken>())).Returns(() =>
        {
            if (Volatile.Read(ref pending) == 1)
                return Task.FromResult<IReadOnlyList<ReadModelOutboxMessage>>(new[] { message });
            idle.TrySetResult();
            return Task.FromResult<IReadOnlyList<ReadModelOutboxMessage>>(Array.Empty<ReadModelOutboxMessage>());
        });
        store.Setup(x => x.ReadCurrentStateAsync(message, It.IsAny<CancellationToken>())).ReturnsAsync(state);
        store.Setup(x => x.ApplyAsync(It.Is<IReadOnlyList<ReadModelProjectionState>>(rows => rows.Count == 1 && rows[0] == state), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        store.Setup(x => x.MarkProcessedAsync(It.Is<IReadOnlyList<ReadModelOutboxMessage>>(messages => messages.Count == 1 && messages[0] == message), It.IsAny<CancellationToken>()))
            .Callback(() => { Interlocked.Exchange(ref pending, 0); projected.TrySetResult(); }).Returns(Task.CompletedTask);
        await using var provider = Provider(store, options);
        using var worker = Worker(provider, options, signal);

        try
        {
            await worker.StartAsync(default);
            await idle.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Interlocked.Exchange(ref pending, 1);
            signal.Notify();
            await projected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { await worker.StopAsync(default); }

        store.Verify(x => x.MarkProcessedAsync(It.IsAny<IReadOnlyList<ReadModelOutboxMessage>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Worker_WithoutNotification_StillPollsAfterTimeout()
    {
        // Kiểm tra polling dự phòng tiếp tục kiểm tra Outbox dù không nhận được tín hiệu trong process.
        var options = Options.Create(new ReadModelSynchronizationOptions { Enabled = true, PollIntervalSeconds = 1 });
        var polled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            if (Interlocked.Increment(ref attempts) == 2)
                polled.TrySetResult();
            return Task.FromResult<IAsyncDisposable?>(null);
        });
        await using var provider = Provider(store, options);
        using var worker = Worker(provider, options, new ReadModelSynchronizationSignal());

        try
        {
            await worker.StartAsync(default);
            await polled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { await worker.StopAsync(default); }

        Assert.True(attempts >= 2);
        store.Verify(x => x.ReadPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Worker_WhenLeaseBusy_DoesNotSpinWhileWaiting()
    {
        // Kiểm tra consumer khác giữ lease thì worker chờ tín hiệu hoặc polling thay vì liên tục xin khóa.
        var options = Options.Create(new ReadModelSynchronizationOptions { Enabled = true, PollIntervalSeconds = 3600 });
        var attempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            Interlocked.Increment(ref attempts);
            attempted.TrySetResult();
            return Task.FromResult<IAsyncDisposable?>(null);
        });
        await using var provider = Provider(store, options);
        using var worker = Worker(provider, options, new ReadModelSynchronizationSignal());
        var starting = Task.Run(() => worker.StartAsync(default));

        try
        {
            await attempted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(100);
            Assert.Equal(1, Volatile.Read(ref attempts));
        }
        finally
        {
            await worker.StopAsync(default);
            await starting.WaitAsync(TimeSpan.FromSeconds(5));
        }

        store.Verify(x => x.ReadPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Worker_WhenSignalsArriveDuringFailure_HonorsRetryBackoff()
    {
        // Kiểm tra tín hiệu liên tục không bỏ qua thời gian nghỉ khi lỗi, đồng thời dừng được trong backoff.
        var options = Options.Create(new ReadModelSynchronizationOptions { Enabled = true, PollIntervalSeconds = 3600 });
        var signal = new ReadModelSynchronizationSignal();
        var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            Interlocked.Increment(ref attempts);
            failed.TrySetResult();
            signal.Notify();
            throw new IOException("database unavailable");
        });
        await using var provider = Provider(store, options);
        using var worker = Worker(provider, options, signal);
        var starting = Task.Run(() => worker.StartAsync(default));

        try
        {
            await failed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            for (var i = 0; i < 10; i++)
            {
                signal.Notify();
                await Task.Delay(10);
            }
            Assert.Equal(1, Volatile.Read(ref attempts));
        }
        finally
        {
            await worker.StopAsync(default);
            await starting.WaitAsync(TimeSpan.FromSeconds(5));
        }

        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Worker_DisposesScopeBeforeNextBatchAndBeforeWaiting()
    {
        // Kiểm tra scope mỗi batch được giải phóng trước batch tiếp theo và trước lúc worker chờ rỗng.
        var options = Options.Create(new ReadModelSynchronizationOptions { Enabled = true, PollIntervalSeconds = 3600 });
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = 0;
        var disposedWhenWaiting = -1;
        var disposedBeforeBatch = new List<int>();
        var reads = 0;
        var message = new ReadModelOutboxMessage();
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            disposedBeforeBatch.Add(Volatile.Read(ref disposed));
            return Task.FromResult<IAsyncDisposable?>(new WorkerLease(() => { }));
        });
        store.Setup(x => x.ReadPendingAsync(100, It.IsAny<CancellationToken>())).Returns(() =>
            Task.FromResult<IReadOnlyList<ReadModelOutboxMessage>>(++reads == 1 ? new[] { message } : Array.Empty<ReadModelOutboxMessage>()));
        store.Setup(x => x.ReadCurrentStateAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadModelProjectionState("test", new Dictionary<string, object?>(), null));
        store.Setup(x => x.ApplyAsync(It.IsAny<IReadOnlyList<ReadModelProjectionState>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        store.Setup(x => x.MarkProcessedAsync(It.IsAny<IReadOnlyList<ReadModelOutboxMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var signal = new Mock<IReadModelSynchronizationSignal>(MockBehavior.Strict);
        signal.Setup(x => x.WaitAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(async (TimeSpan _, CancellationToken token) =>
            {
                disposedWhenWaiting = Volatile.Read(ref disposed);
                waiting.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
            });
        await using var provider = new ServiceCollection()
            .AddScoped(_ => new ScopeProbe(() => Interlocked.Increment(ref disposed)))
            .AddScoped(sp =>
            {
                sp.GetRequiredService<ScopeProbe>();
                return new ReadModelOutboxProcessor(store.Object, options);
            }).BuildServiceProvider();
        using var worker = Worker(provider, options, signal.Object);

        try
        {
            await worker.StartAsync(default);
            await waiting.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { await worker.StopAsync(default); }

        Assert.Equal(new[] { 0, 1 }, disposedBeforeBatch);
        Assert.Equal(2, disposedWhenWaiting);
        Assert.Equal(2, disposed);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task StopWorker_WhileIdle_CancelsSignalWaitGracefully()
    {
        // Kiểm tra dừng worker rỗng hủy lần chờ tín hiệu dài và hoàn tất host bình thường.
        var options = Options.Create(new ReadModelSynchronizationOptions { Enabled = true, PollIntervalSeconds = 3600 });
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = false;
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkerLease(() => { released = true; idle.TrySetResult(); }));
        store.Setup(x => x.ReadPendingAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ReadModelOutboxMessage>());
        await using var provider = Provider(store, options);
        using var worker = Worker(provider, options, new ReadModelSynchronizationSignal());

        try
        {
            await worker.StartAsync(default);
            await idle.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { await worker.StopAsync(default).WaitAsync(TimeSpan.FromSeconds(5)); }

        Assert.True(released);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
        store.Verify(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Worker_WhenStoreFails_RetriesWithoutTerminatingHost()
    {
        // Kiểm tra lỗi Write/Read DB trong một lượt không làm chết worker và lượt sau được thử lại.
        var options = Options.Create(new ReadModelSynchronizationOptions { Enabled = true, PollIntervalSeconds = 1 });
        var retried = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
                throw new IOException("database unavailable");
            retried.TrySetResult();
            return Task.FromResult<IAsyncDisposable?>(null);
        });
        await using var provider = new ServiceCollection().AddSingleton<IOptions<ReadModelSynchronizationOptions>>(options)
            .AddSingleton(store.Object).AddScoped<ReadModelOutboxProcessor>().BuildServiceProvider();
        using var worker = new ReadModelSynchronizationWorker(provider.GetRequiredService<IServiceScopeFactory>(), options,
            NullLogger<ReadModelSynchronizationWorker>.Instance);

        await worker.StartAsync(default);
        await retried.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(default);

        Assert.True(attempts >= 2);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task StopWorker_DuringSourceRead_ReleasesLeaseAndLeavesPending()
    {
        // Kiểm tra dừng host trong lúc đọc nguồn giải phóng khóa và không ghi replica hay ack.
        var options = Options.Create(new ReadModelSynchronizationOptions { Enabled = true, PollIntervalSeconds = 1 });
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = false;
        var message = new ReadModelOutboxMessage();
        var store = new Mock<IReadModelProjectionStore>(MockBehavior.Strict);
        store.Setup(x => x.AcquireConsumerLockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkerLease(() => released = true));
        store.Setup(x => x.ReadPendingAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { message });
        store.Setup(x => x.ReadCurrentStateAsync(message, It.IsAny<CancellationToken>()))
            .Returns(async (ReadModelOutboxMessage _, CancellationToken token) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
                return null!;
            });
        await using var provider = new ServiceCollection().AddSingleton<IOptions<ReadModelSynchronizationOptions>>(options)
            .AddSingleton(store.Object).AddScoped<ReadModelOutboxProcessor>().BuildServiceProvider();
        using var worker = new ReadModelSynchronizationWorker(provider.GetRequiredService<IServiceScopeFactory>(), options,
            NullLogger<ReadModelSynchronizationWorker>.Instance);

        await worker.StartAsync(default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(default);

        Assert.True(released);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
        store.Verify(x => x.ApplyAsync(It.IsAny<IReadOnlyList<ReadModelProjectionState>>(), It.IsAny<CancellationToken>()), Times.Never);
        store.Verify(x => x.MarkProcessedAsync(It.IsAny<IReadOnlyList<ReadModelOutboxMessage>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ServiceProvider Provider(Mock<IReadModelProjectionStore> store, IOptions<ReadModelSynchronizationOptions> options) =>
        new ServiceCollection().AddSingleton(options).AddSingleton(store.Object).AddScoped<ReadModelOutboxProcessor>().BuildServiceProvider();

    private static ReadModelSynchronizationWorker Worker(ServiceProvider provider, IOptions<ReadModelSynchronizationOptions> options,
        IReadModelSynchronizationSignal signal) => new(provider.GetRequiredService<IServiceScopeFactory>(), options,
            NullLogger<ReadModelSynchronizationWorker>.Instance, signal);

    private sealed class ScopeProbe(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }

    private sealed class WorkerLease(Action dispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            dispose();
            return ValueTask.CompletedTask;
        }
    }
}
