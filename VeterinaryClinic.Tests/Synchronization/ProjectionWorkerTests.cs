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

    private sealed class WorkerLease(Action dispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            dispose();
            return ValueTask.CompletedTask;
        }
    }
}
