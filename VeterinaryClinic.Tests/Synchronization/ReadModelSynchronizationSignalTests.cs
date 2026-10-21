using VeterinaryClinic.Infrastructure.Synchronization;
using Xunit;

namespace VeterinaryClinic.Tests.Synchronization;

public sealed class ReadModelSynchronizationSignalTests
{
    [Fact]
    public async Task Notify_BeforeWaiting_PreservesWakeup()
    {
        // Kiểm tra tín hiệu phát trước khi worker chờ vẫn được giữ cho lần chờ kế tiếp.
        var signal = new ReadModelSynchronizationSignal();

        signal.Notify();
        await WaitForNotificationAsync(signal);
    }

    [Fact]
    public async Task Notify_DuringWaiting_CompletesWait()
    {
        // Kiểm tra tín hiệu mới đánh thức lần chờ đang diễn ra mà không đợi timeout polling.
        var signal = new ReadModelSynchronizationSignal();
        using var cancellation = new CancellationTokenSource();
        var waiting = signal.WaitAsync(TimeSpan.FromHours(1), cancellation.Token);

        try
        {
            Assert.False(waiting.IsCompleted);
            signal.Notify();
            await waiting.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { cancellation.Cancel(); }
    }

    [Fact]
    public async Task ConcurrentNotifications_CoalesceIntoOneWakeup()
    {
        // Kiểm tra nhiều producer cùng phát tín hiệu chỉ giữ một wakeup, không tạo hàng đợi tăng vô hạn.
        var signal = new ReadModelSynchronizationSignal();
        Parallel.For(0, 1000, _ => signal.Notify());
        await WaitForNotificationAsync(signal);
        using var cancellation = new CancellationTokenSource();
        var next = signal.WaitAsync(TimeSpan.FromHours(1), cancellation.Token);

        try { Assert.False(next.IsCompleted); }
        finally { cancellation.Cancel(); }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => next);
    }

    [Fact]
    public async Task Wait_WhenTimedOut_DoesNotConsumeLaterNotification()
    {
        // Kiểm tra timeout không để lại reader mồ côi nuốt tín hiệu dành cho lần chờ sau.
        var signal = new ReadModelSynchronizationSignal();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await signal.WaitAsync(TimeSpan.FromMilliseconds(20), cancellation.Token);
        signal.Notify();
        await WaitForNotificationAsync(signal);
    }

    [Fact]
    public async Task Wait_WhenCancelled_DoesNotConsumeLaterNotification()
    {
        // Kiểm tra hủy lần chờ trả lỗi cancellation và tín hiệu về sau vẫn đánh thức lần chờ mới.
        var signal = new ReadModelSynchronizationSignal();
        using var cancellation = new CancellationTokenSource();
        var waiting = signal.WaitAsync(TimeSpan.FromHours(1), cancellation.Token);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        signal.Notify();
        await WaitForNotificationAsync(signal);
    }

    [Fact]
    public async Task Wait_WithAlreadyCancelledToken_PreservesBufferedNotification()
    {
        // Kiểm tra token đã hủy không được tiêu thụ tín hiệu đang có sẵn trong bộ đệm.
        var signal = new ReadModelSynchronizationSignal();
        signal.Notify();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => signal.WaitAsync(TimeSpan.FromHours(1), cancellation.Token));
        await WaitForNotificationAsync(signal);
    }

    private static async Task WaitForNotificationAsync(ReadModelSynchronizationSignal signal)
    {
        using var cancellation = new CancellationTokenSource();
        try { await signal.WaitAsync(TimeSpan.FromHours(1), cancellation.Token).WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { cancellation.Cancel(); }
    }
}
