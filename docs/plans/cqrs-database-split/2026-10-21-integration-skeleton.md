# Skeleton tích hợp wakeup (Markdown)

runId: read-write-lag-20261021
taskId: Host-integration
skeletonRevision: 1
planRevision: 1
specRevision: 1
overviewRevision: 1

## File và signature

- ReadModelSynchronizationServiceCollectionExtensions.AddReadModelSynchronization(IServiceCollection): IServiceCollection — thêm singleton IReadModelSynchronizationSignal / ReadModelSynchronizationSignal trước scoped processor và hosted worker. Không mở DB.
- DatabaseServiceExtensionsTests — thêm test split tạo hai scope, resolve notifier cùng instance; write context constructor được DI cấp notifier; read/write connection vẫn closed. Test disabled Assert.DoesNotContain registration notifier.
- ReadModelIntegrationTests — tạo CountingSignal thực thi interface, truyền vào source context optional param. Sau source save số notify tăng đúng một; sau processor apply/ack số notify không tăng. Giữ assertions identity/audit/insert-update-delete cũ.
- README.md — cập nhật bước 2 và paragraph vận hành, ghi rõ wakeup chỉ trong process, polling fallback/retry, eventual consistency còn giữ.

## Luồng

DI singleton → optional write context producer + worker consumer. Capture marker → transaction commit → Notify → WaitAsync được đánh thức → batch có dữ liệu → batch tiếp ngay → batch rỗng → WaitAsync. Apply/ack không đánh dấu capture. Failure nghỉ theo PollIntervalSeconds. Mọi nguồn dữ liệu thực vẫn lấy từ Outbox, không payload trong signal.

Không có executable stub trong skeleton. Chỉ host giữ quyền source/test/README trong registry. Compile/test chỉ sau workers báo contract và test-ready để không tranh chấp output.
