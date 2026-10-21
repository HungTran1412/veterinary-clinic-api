# Phát tín hiệu đồng bộ sau commit Outbox

runId: read-write-lag-20261021
taskId: T01-commit-signal
specRevision: 1
overviewRevision: 1
sourceRefs: 2026-10-21-overview.md/AC01,AC05,AC06; 2026-10-20-overview.md/D01,D03; VeterinaryClinicDataContext.SaveChanges/SaveChangesAsync; ReadModelOutboxCapture.SaveChanges/SaveChangesAsync; OutboxCaptureTests.CallerRollback_RemovesBusinessAndOutboxFromMultipleSaves,CallerSavepointFailure_PreservesEarlierSaveAndCanCommitRetry,AcknowledgingOutbox_DoesNotEmitAnotherMessage.

## Yêu cầu và giới hạn

AC01: phát một tín hiệu best-effort chỉ sau transaction thực tế commit thành công và transaction đó đã capture Outbox. Bao phủ bốn overload SaveChanges, transaction tự mở và caller mở, SaveChanges(false). AC05: cùng contract singleton do host đăng ký, không phát vì ack Outbox, no-op hoặc khi disabled. AC06: test backend UTF-8 có comment tiếng Việt đầu hàm, không cần database ngoài. Không đổi schema, raw SQL, migration, audit, API/DTO hoặc cách accept/recover hiện tại; chỉ sửa các đường dẫn được giao.

## Hiện trạng có chứng cứ

Capture tự commit transaction sở hữu sau khi ghi business rồi Outbox; transaction caller chỉ release savepoint nên trả SaveChanges thành công chưa có nghĩa đã commit. Lần save lỗi rollback transaction sở hữu hoặc rollback savepoint và khôi phục tracker. Ack Outbox không thuộc ChangedBusinessEntries nên không tạo message. Write context chưa có tín hiệu/interceptor. Test stack là .NET 8/xUnit/Moq với SQLite :memory: và assertions xUnit, sử dụng provider relational để chứng minh thời điểm transaction.

## Hành vi đề xuất

- Thêm public interface `IReadModelSynchronizationSignal` trong Data.Synchronization: `void Notify()` và `Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)` đúng contract host. Không payload, không chờ worker.
- Constructor write context thêm tham số optional signal sau synchronizationOptions, giữ caller hiện có tương thích. Chỉ tạo interceptor per-context khi capture enabled và signal khác null; OnConfiguring thêm interceptor vào options. Host chỉ inject singleton, không cần thêm interceptor vào DI.
- Capture nhận callback optional nội bộ đánh dấu actual DbTransaction. Với owned transaction: đánh dấu sau khi insert hết Outbox, trước commit. Với external transaction: chỉ đánh dấu sau ReleaseSavepoint thành công, để lỗi lần save chưa thành công không tạo marker.
- Interceptor giữ reference transaction có Outbox, không dùng boolean chung. TransactionCommitted/Async chỉ notify khi reference trùng marker, rồi xóa marker trước notify. TransactionRolledBack/Async xóa marker tương ứng. Transaction mới/ack-only không trùng marker cũ, nên disposal hoặc transaction cũ không làm phát tín hiệu cho transaction mới. Nhiều SaveChanges trong một external transaction gộp thành một Notify sau commit.
- Notify được bọc catch tại producer: lỗi notifier không đổi kết quả business đã commit. Lỗi persist/insert/release/commit không phát tín hiệu; rollback savepoint của lần save sau không xóa marker từ lần save trước đã thành công. Cancellation trước commit không phát. Chế độ enabled nhưng chưa inject signal vẫn lưu Outbox như cũ và polling dự phòng tiếp tục hoạt động.
- Quyền truy cập/domain/API/UI không đổi. Data không phụ thuộc Infrastructure. Worker T02 consume interface; host sở hữu đăng ký singleton và kiểm chứng DI.

## Test plan và nghiệm thu

| Subject | Nhóm áp dụng | Kịch bản | Hành vi được bảo vệ |
| --- | --- | --- | --- |
| SaveChanges bốn overload | Happy path, Side effects, State, Output | Owned commit, kể cả accept=false | Business + Outbox đã tồn tại khi Notify; chỉ một tín hiệu |
| External transaction | Transaction, Boundary, Side effects | Nhiều save; commit sync/async; rollback/dispose | Không notify trước commit; một notify sau commit; rollback không notify |
| Failure/retry | Error, Transaction, State | Lỗi insert Outbox; failed commit; canceled save; failed savepoint sau save hợp lệ | Không tín hiệu cho lần lỗi; transaction trước còn hợp lệ vẫn notify khi commit |
| Marker isolation | Boundary, Side effects | Rollback/dispose transaction có Outbox rồi commit transaction ack-only/no-op | Marker cũ không đánh thức transaction mới |
| Disabled/no signal/ack | Boundary, Side effects | Capture disabled hoặc signal null; ack và no-op | Tương thích caller cũ; không tín hiệu hoặc vòng lặp |
| Throwing notifier | Error, Output | Notify ném exception sau commit sync/async | SaveChanges/caller commit vẫn thành công; dữ liệu đã commit đúng |

Validation và Authorization/Permission không áp dụng: đây là observer transaction nội bộ, không thêm kiểm tra người dùng hay thay nghiệp vụ. Business rules chỉ có tính nguyên tử và thời điểm side effect nêu trên. Kiểm thử dùng SQLite :memory: riêng từng fixture, tuyệt đối không mở DB hệ thống. Không chứng minh SQL Server runtime hoặc callback khi caller gọi trực tiếp underlying DbTransaction.Commit thay vì EF IDbContextTransaction; các caller repo dùng API transaction của EF và polling vẫn dự phòng cho đường ghi ngoài capture.

Không có câu hỏi nghiệp vụ hay thay đổi ownership đang chặn. Gửi host duyệt Spec revision 1; chưa viết plan/source/tests.
