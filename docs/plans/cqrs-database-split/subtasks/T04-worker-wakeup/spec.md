# Đặc tả worker đánh thức và xử lý liên tục

runId: read-write-lag-20261021
taskId: T02-worker-wakeup
specRevision: 1
overviewRevision: 1
workerId: /root/worker_wakeup
sourceRefs: VeterinaryClinic.Infrastructure/Synchronization/ReadModelSynchronizationWorker.cs: ExecuteAsync; ReadModelOutboxProcessor.cs: ProcessBatchAsync; VeterinaryClinic.Tests/Synchronization/ProjectionWorkerTests.cs; ProjectionProcessorTests.cs; VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj; docs/plans/cqrs-database-split/2026-10-20-overview.md.

## Yêu cầu và giới hạn

Áp dụng task-worker và backend-unit-test trong InvocationContext host-managed theo TaskPacket. Chỉ sửa signal implementation, worker, hai file test được giao và tài liệu tại artifactRoot. Không sửa Data/interface/producer, processor/SQL/lease, DI, cấu hình, API, migration hoặc DB ngoài; không commit. Nguồn miền dự án dùng tài liệu CQRS ngày 20/10 và overview ngày 21/10 vì không có Wiki riêng.

## Hiện trạng có chứng cứ

Worker tạo scope và chạy một ProcessBatchAsync rồi luôn nghỉ PollIntervalSeconds; không sử dụng số lượng processed trả về. Processor đã trả 0 khi lease busy hoặc pending rỗng, trả pending.Count sau apply và ack thành công, đồng thời giải phóng lease trước return/throw. Bộ test hiện dùng xUnit 2.5.3/Moq/net8.0, mock store và real processor, không mở DB. Ba test worker cũ bảo vệ disabled, retry và cancellation trong source read.

## Hành vi đề xuất

- Implement IReadModelSynchronizationSignal theo contract host/T01 trong namespace Data: Notify không chặn; WaitAsync(TimeSpan, CancellationToken) chờ signal, timeout hoặc cancellation. Implementation Infrastructure dùng Channel dung lượng 1, nhiều writer, một reader, gộp signal. Notify trước WaitAsync vẫn đánh thức được lần chờ tiếp theo.
- Wait timeout phải hủy và quan sát thao tác chờ thật để không còn reader mồ côi nuốt tín hiệu tương lai. Cancellation token caller vẫn ném OperationCanceledException, kể cả signal đã có sẵn. Timeout hoàn tất bình thường.
- Worker nhận signal làm tham số cuối tùy chọn để giữ tương thích constructor ba tham số hiện có; trường hợp không được DI cấp signal vẫn polling như trước. Host cấp singleton dùng chung khi tích hợp.
- Enabled=false thoát trước tạo scope hoặc wait. Mỗi vòng tạo scope mới, await ProcessBatchAsync, giải phóng scope rồi quyết định tiếp. processed > 0 tiếp tục ngay, bao gồm batch nhỏ hơn BatchSize. processed=0 chờ signal hoặc timeout polling; lease busy đi cùng nhánh này để tránh tự quay vòng.
- Lỗi process/resolve/dispose vẫn log và nghỉ Task.Delay bằng polling, bỏ qua signal trong lần backoff này để producer liên tục không làm retry nóng. Signal đang chờ vẫn có thể dùng khi vòng bình thường tiếp theo rỗng.
- Cancellation trong xử lý, chờ signal hoặc backoff kết thúc graceful. Giữ nguyên processor/lease/ack và giới hạn mỗi batch. Không cam kết đọc thấy ngay hoặc độ trễ SQL Server cụ thể.

## Kiểm chứng và tiêu chí

| Method | Nhóm áp dụng | Kịch bản chính | Hành vi bảo vệ |
| --- | --- | --- | --- |
| Signal.Notify/WaitAsync | Happy path, state, boundary, cancellation | Notify trước/sau wait; nhiều notify gộp; timeout rồi notify; cancel rồi notify; token đã cancel | Không mất wakeup, không reader mồ côi; signal không giữ payload; cancellation đúng contract |
| Worker.ExecuteAsync | Happy path, side effect, lifecycle, boundary | Hai batch một message rồi rỗng với polling=3600; idle wake bằng real signal; timeout polling; lease busy | Không nghỉ giữa batch có dữ liệu (AC02); signal và fallback còn hoạt động (AC03); không spin khi busy (AC04) |
| Worker.ExecuteAsync | Error path, cancellation, lifecycle | Store lỗi trong khi flood signal; dừng khi idle; scope disposed trước wait/batch kế; giữ ba test hiện có | Backoff không bị signal bỏ qua, scope/lease được giải phóng, host dừng sạch (AC04/06) |

Validation của dữ liệu business, authorization, transaction, SQL và UI không áp dụng cho các subject này. Processor có tests riêng, không viết lặp lại logic apply/ack. Mọi test mới có comment tiếng Việt ở đầu hàm, UTF-8. Test dùng mock store, real signal, TCS và timeout 5 giây để giới hạn lỗi; polling dài chứng minh không chờ chu kỳ. Retry/fallback dùng polling 1 giây với timeout đủ rộng, không đo SLA bằng vài millisecond.

Host điều phối chạy test để tránh build đồng thời. Gửi test-ready trước red; host chạy regression drain trên worker cũ để quan sát thất bại rồi cho phép implementation. Sau implementation host chạy worker+signal slice và toàn bộ backend. AC06 thuộc worker hoàn tất khi test được host chạy và review; không tự ghi Passed cho check chưa chạy.

## Phụ thuộc và câu hỏi

Phụ thuộc T01-commit-signal Spec approved cho contract IReadModelSynchronizationSignal trước source/test edit dùng contract đó. Không có câu hỏi nghiệp vụ còn chặn; mọi thay đổi intended behavior/ownership gửi host theo C5. Trình Spec revision 1 để host duyệt trước lập plan.
