# Skeleton producer commit signal

runId: read-write-lag-20261021
taskId: T01-commit-signal
skeletonRevision: 1
planRevision: 1
specRevision: 1
overviewRevision: 1

## Các chữ ký dự kiến

- Data.Synchronization.IReadModelSynchronizationSignal: public void Notify(); Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken).
- VeterinaryClinicDataContext constructor: giữ options/contextAccessorFactory/options sync, thêm optional IReadModelSynchronizationSignal? synchronizationSignal = null ở cuối. Readonly interceptor và callback field null khi disabled hoặc không inject. Override OnConfiguring(DbContextOptionsBuilder) gọi base rồi thêm interceptor được tạo cho context đó.
- ReadModelOutboxCapture.SaveChanges: thêm optional Action<DbTransaction>? outboxCaptured = null sau persist; async thêm callback optional sau cancellationToken, giữ caller hiện tại tương thích.
- ReadModelOutboxTransactionInterceptor: internal sealed, constructor(signal), private DbTransaction? pendingTransaction; MarkCaptured(DbTransaction); override TransactionCommitted/Async và TransactionRolledBack/Async. Committed matching ref: clear marker rồi try Notify, catch để business result không bị thay đổi; rolled back matching ref clear. Async callback trả Task.CompletedTask, không dùng token cancellation để hủy postcommit notify.

## Trình tự

Owned save: persist(false) → insert từng message → callback(actual transaction) → EF Commit/CommitAsync → interceptor postcommit matching → Notify best-effort → AcceptAllChanges nếu caller yêu cầu. External save: persist(false) → insert messages → ReleaseSavepoint thành công → callback(actual transaction) → SaveChanges trả → caller commit thực tế → postcommit matching → Notify một lần. Không có business changes: persist như cũ, không callback. Recover/savepoint SQL không thay đổi.

## Skeleton tests

OutboxCommitSignalTests fixture: SQLite memory database tên GUID với hai connection Mode=Memory;Cache=Shared. Write context sync enabled + mock signal; observer context disabled trên connection riêng. Notify callback lưu kết quả observer đọc business và Outbox hoặc exception để assertion ngoài callback chứng minh đã commit. Mọi fixture độc lập, dispose cả context/connection; không file/database ngoài.

Các test dự kiến:

1. OwnedCommit_NotifiesAfterBusinessAndOutboxPersisted: Theory bốn overload và accept=false; count/save result + observer snapshot + Notify once. Đây là regression đầu tiên sau contract-only scaffolding; host chạy red trước runtime logic.
2. CallerCommit_AfterMultipleSaves_NotifiesOnce: Theory commit sync/async, save mixed sync/async/false; assert zero trước commit, hai row+Outbox sau commit, một notify.
3. CallerRollbackOrDispose_DoesNotNotify: Theory sync/async rollback và dispose; observer rỗng, zero notify.
4. FailedCaptureOrCommit_DoesNotNotifyAndAllowsRetry: lỗi Outbox trigger, business constraint hoặc interceptor throw trước actual commit; assert rollback business+Outbox, signal zero; bỏ lỗi rồi retry đúng một notify.
5. FailedSavepointAfterCapturedSave_PreservesPendingNotification: save đầu hợp lệ, save sau fail Outbox → rollback savepoint → caller commit giữ row đầu và signal một.
6. CancellationAfterBusinessSave_DoesNotNotify: SaveChangesInterceptor hủy token một lần; assert không dữ liệu/Outbox/signal rồi retry một notify.
7. AcknowledgementAndNoChanges_DoNotNotify: seed save rồi reset mock, ack message và no-op; không phát thêm.
8. PreviousTransactionMarker_DoesNotNotifyNewAckTransaction: seed pending message, reset signal, capture business trong tx rồi dispose/rollback; tx mới chỉ ack seed message rồi commit, signal zero và observer chỉ row seed.
9. DisabledCaptureAndMissingSignal_KeepLegacySaveBehavior: enabled=false+signal không notify/không Outbox; enabled=true+null signal vẫn Outbox; observer đúng business.
10. ThrowingNotifier_DoesNotFailCommittedSaveOrCallerCommit: Theory owned/external, sync/async; signal throw nhưng business+Outbox đã commit, save/caller commit không throw.

Không executable stubs ở giai đoạn này. G01 dùng skeletonRevision=1, chờ host Skeleton1 approval + GroupPlan revalidation trước C4.
