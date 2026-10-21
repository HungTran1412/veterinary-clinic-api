# Kế hoạch worker đánh thức và xử lý liên tục

runId: read-write-lag-20261021
taskId: T02-worker-wakeup
planRevision: 1
specRevision: 1
overviewRevision: 1

## 1. Bối cảnh

Spec1 đã được host duyệt tại T02-worker-wakeup-SPEC-01; T01-commit-signal Spec1 duyệt cùng ledger ngày 21/10, đủ phụ thuộc contract. Kế hoạch chỉ hiện thực hành vi trong spec, không mở rộng thành synchronous projection hoặc read-your-writes.

## 2. Giới hạn

Size M thừa kế. Quyền ghi: ReadModelSynchronizationSignal.cs mới, ReadModelSynchronizationWorker.cs, ProjectionWorkerTests.cs, ReadModelSynchronizationSignalTests.cs mới và artifactRoot này. Các file còn lại thuộc host/T01 hoặc bị cấm. Không chạy API, DB ngoài, migration, commit; host điều phối mọi build/test.

## 3. Quy ước

C# net8 primary constructor; xUnit/Moq; test thật worker/processor/signal và mock projection store, TCS RunContinuationsAsynchronously, timeout 5 giây giới hạn kịch bản. Mỗi test có comment tiếng Việt UTF-8 đầu hàm; helper chỉ phục vụ setup/cleanup, không che behavior assertions. Áp dụng backend-unit-test và guide đã đọc.

## 4. Hợp đồng

IReadModelSynchronizationSignal do T01 sở hữu tại namespace VeterinaryClinic.Data.Synchronization: void Notify(), Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken). Notify nonblocking và coalesce; Wait timeout hoàn tất bình thường; caller cancellation ném OperationCanceledException. Worker thêm signal nullable optional ở cuối constructor để test/consumer cũ compile; host DI đăng ký singleton thực tế.

## 5. Luồng

Bounded Channel một phần tử lưu wakeup. Mỗi Notify gọi TryWrite. Mỗi WaitAsync kiểm tra caller token trước, tạo linked CTS + CancelAfter(timeout), await trực tiếp read với token đó; chỉ nuốt cancellation do timeout khi caller chưa hủy. Đợi read kết thúc trước disposal CTS, không dùng Task.WhenAny bỏ lại read mồ côi.

Worker giữ processed biến ngoài scope: process trong await using scope, thoát block rồi nếu processed>0 continue; nếu =0 thì signal.WaitAsync hoặc Task.Delay nếu signal=null. Exception log và chờ Task.Delay poll độc lập signal; catch cancellation có điều kiện stoppingToken tại xử lý và chờ để ExecuteTask kết thúc thành công.

## 6. File

| Đường dẫn | Trách nhiệm | Owner |
| --- | --- | --- |
| VeterinaryClinic.Infrastructure/Synchronization/ReadModelSynchronizationSignal.cs | Channel signal, timeout/cancel an toàn | /root/worker_wakeup |
| VeterinaryClinic.Infrastructure/Synchronization/ReadModelSynchronizationWorker.cs | Drain/wait/backoff/lifecycle | /root/worker_wakeup |
| VeterinaryClinic.Tests/Synchronization/ProjectionWorkerTests.cs | Regression drain và worker behavior | /root/worker_wakeup |
| VeterinaryClinic.Tests/Synchronization/ReadModelSynchronizationSignalTests.cs | Signal before/after/coalesce/timeout/cancel | /root/worker_wakeup |

## 7. Nhóm triển khai

Một nhóm [G01](groups/G01.md) do không có consumer độc lập cần tách bàn giao. Trình tự nội bộ: viết drain regression tương thích ctor cũ; host chạy red; hiện thực signal/worker và tests còn lại sau red; host chạy green. Không chỉnh processor để làm test pass.

## 8. Kiểm chứng

- AC02: regression hai partial batches + empty với poll 3600, target TCS trước timeout 5 giây; worker cũ phải timeout, worker mới hoàn tất và chỉ ack 2 message.
- AC03: real signal notify trước/sau wait, nhiều notify gộp; timeout/cancel không mất signal về sau; worker idle được wake với poll3600; polling1 vẫn retry khi không Notify.
- AC04: lease busy không tự loop; failure không bỏ backoff dù signal có sẵn/flood; stop idle/backoff graceful; scope disposed trước wait/batch tiếp; giữ test source-read lease cancellation.
- AC06: host chạy filter worker/signal/processor sau green, sau đó backend suite/build; diff review comment/UTF-8 và phạm vi. Không ghi Passed khi check chưa chạy.

Nhóm validation/authorization/transaction SQL không áp dụng; transaction/ack test do processor/T01/host sở hữu. Không thêm package, cấu hình coverage hoặc UI tests.

## 9. Tích hợp

Host cấp cùng singleton cho producer và worker, chạy capture→projection→ack và DI identity tests. Worker phụ thuộc interface đúng contract; signal không payload, Outbox luôn là nguồn bền vững. Polling khi restart/khác instance/mất tín hiệu giữ nguyên.

## 10. Rủi ro và phục hồi

Signal in-process không làm strong consistency; latency SQL thật chưa đo. Gộp signal có thể gây tối đa một lượt kiểm tra rỗng dư; không làm mất dữ liệu nhờ Outbox/polling. Lỗi dùng poll backoff ngăn tải tăng. Không schema/data change nên rollback source là đủ khi chưa deploy; không có quyền vận hành deployment.

## 11. Phê duyệt

Ledger host: ../../2026-10-21-tracking.md. Chờ Plan1 và GroupPlan1; sau đó mới viết Markdown skeleton. Source/tests chỉ sau current Spec+Plan+Skeleton+GroupPlan và host xác nhận lịch red.

## 12. Hoàn tất

GroupResult và FinalResult ghi actual test evidence do host cung cấp, các artifact revisions, consistency spec↔code và giới hạn SQL Server. Host là chủ thể quyết định Done sau kiểm tra tích hợp.
