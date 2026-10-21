# Skeleton Markdown: signal và worker

runId: read-write-lag-20261021
taskId: T02-worker-wakeup
skeletonRevision: 1
planRevision: 1
specRevision: 1
overviewRevision: 1

## File và chữ ký dự kiến

| Path | Trách nhiệm | Signature / contract | Group |
| --- | --- | --- | --- |
| VeterinaryClinic.Infrastructure/Synchronization/ReadModelSynchronizationSignal.cs | Channel bounded1, coalesced signal | sealed class ReadModelSynchronizationSignal : IReadModelSynchronizationSignal; public void Notify(); public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) | G01 |
| VeterinaryClinic.Infrastructure/Synchronization/ReadModelSynchronizationWorker.cs | Drain sau positive processed; wait khi 0; lỗi backoff | constructor(scopeFactory, options, logger, IReadModelSynchronizationSignal? signal = null); protected override Task ExecuteAsync(CancellationToken stoppingToken) | G01 |
| VeterinaryClinic.Tests/Synchronization/ProjectionWorkerTests.cs | Test worker+real processor với mock store | Fact drain partial, idle wake, fallback, lease busy, scope disposal, failure flood, idle stop; giữ test cũ | G01 |
| VeterinaryClinic.Tests/Synchronization/ReadModelSynchronizationSignalTests.cs | Signal behavior thật | Fact before wait, during wait, coalesce, timeout/cancel no orphan, already-canceled token | G01 |

## Luồng và test seams

Signal lưu một byte/bool trong Channel. ChannelOptions: SingleReader=true, SingleWriter=false, capacity1, FullMode=DropWrite, AllowSynchronousContinuations=false. Notify chỉ TryWrite, không await. WaitAsync kiểm tra token trước, linked CTS kế thừa cancellation caller và CancelAfter(timeout), await Channel.Reader.ReadAsync; catch cancellation do timeout nếu caller chưa hủy. Không Task.WhenAny với read chưa hủy.

Worker pseudocode minh họa trong Markdown:

```text
if disabled: return
while not stopping:
    try:
        scoped block: processed = await processor.ProcessBatchAsync(stopping)
        if processed > 0: continue
        wait signal-or-poll (fallback Delay when signal absent)
    catch canceled when stopping: break
    catch error:
        log
        wait polling Delay ignoring signal; canceled when stopping -> break
```

Scope kết thúc trước positive continue và trước wait. Lease do processor quản lý giữ nguyên. Không song song hai ProcessBatchAsync. Producer không gọi vào worker trực tiếp và không chứa message payload.

Red đầu tiên compile trên worker cũ: test dùng ctor3 args, polling3600, store.ReadPendingAsync lần1/2 một message, lần3 empty đánh dấu TCS; await TCS với timeout5; finally Stop. Code cũ chỉ chạy lần1 và timeout. Green thêm real signal và test còn lại sau host cung cấp interface source.

Tests mới dùng Strict mock store, real processor, helpers lease/provider; cancellation cleanup nằm finally để thất bại assertion không để worker chạy nền. TCS RunContinuationsAsynchronously chứng minh thứ tự sự kiện, polling dài chứng minh bỏ delay. Failure flood test có polling1 và cho signal liên tục trong cửa sổ trước retry; khẳng định không thêm attempt trước backoff, sau poll vẫn retry. Lease busy không signal phải giữ đúng một attempt đến stop trước poll3600. Scope disposal kiểm tra observer scoped đã dispose trước wait/next acquisition; không mở kết nối DB.

## Yêu cầu review

Đề nghị host duyệt Skeleton1 và revalidate GroupPlan1 G01 với skeletonRevision1; Spec1/Plan1 không thay hành vi. Source/test chưa tạo. Sau approval chỉ thêm red drain rồi báo host chạy red; implementation chờ host xác nhận red theo G01.
