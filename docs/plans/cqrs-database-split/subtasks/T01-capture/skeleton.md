# Markdown skeleton T01

runId: cqrs-db-split-20261020
taskId: T01-capture
skeletonRevision: 1
planRevision: 1
specRevision: 3
overviewRevision: 3

## Proposed files and signatures

| Path | Responsibility | Signature / contract | Group |
| --- | --- | --- | --- |
| VeterinaryClinic.Data/Systems/DataContext/VeterinaryClinicDataContext.cs | Public DbSet/options, mapping, audit và overload routing | constructor(DbContextOptions<VeterinaryClinicDataContext>, Func<IContextAccessor>, IOptions<ReadModelSynchronizationOptions>? = null); DbSet<ReadModelOutboxMessage> ReadModelOutboxMessages; override int SaveChanges(); override int SaveChanges(bool); override Task<int> SaveChangesAsync(CancellationToken); override Task<int> SaveChangesAsync(bool,CancellationToken); private void ApplyAudit() | G01 |
| VeterinaryClinic.Data/Synchronization/ReadModelSynchronizationOptions.cs | Options public dùng host/T02 | sealed class; SectionName constant; Enabled/PollIntervalSeconds/BatchSize đúng spec | G01 |
| VeterinaryClinic.Data/Synchronization/ReadModelOutboxMessage.cs | Public persisted message | sealed class với Id/EntityType/KeyValuesJson/CreatedAtUtc/ProcessedAtUtc đúng spec | G01 |
| VeterinaryClinic.Data/Synchronization/ReadModelOutboxCapture.cs | Atomic orchestration, metadata payload, DbCommand | internal static class; SaveChanges(DbContext,bool,Func<bool,int>); SaveChangesAsync(DbContext,bool,Func<bool,CancellationToken,Task<int>>,CancellationToken); CaptureEntries; CreateInsertCommand; rollback helpers | G01 |
| VeterinaryClinic.Data/Synchronization/OutboxEntrySnapshot.cs | Khôi phục EF state khi persistence thất bại | internal sealed class; constructor(EntityEntry); EntityEntry Entry; void Restore() | G01 |
| VeterinaryClinic.Tests/Synchronization/OutboxCaptureTests.cs | Behavior tests SQLite biệt lập | xUnit Theory/Fact, nested disposable in-memory fixture và SavedChangesInterceptor cancel-once | G01 |

## Data flow and test seams

`SaveChanges()` chỉ forward `SaveChanges(true)`; async token forward `SaveChangesAsync(true,token)`. Bool override disabled: ApplyAudit rồi gọi trực tiếp base bool overload. Enabled: helper nhận delegate lambda gọi audit+base bool overload, không gọi base overload không bool để tránh virtual re-entry. Helper no-business path gọi delegate với original accept boolean; business path luôn delegate(false).

Snapshot lấy toàn bộ changed entries trước audit. Business list loại ReadModelOutboxMessage, lấy key metadata. Guard ambient trước IO; guard caller SupportsSavepoints trước business save. Savepoint tên prefix + Guid tối đa31 ký tự. Local transaction do helper sở hữu được dispose; external transaction chỉ savepoint. Parameterized INSERT có EntityType/KeyValuesJson/CreatedAtUtc, không set Id/ProcessedAtUtc.

Thành công: target local transaction commit hoặc release savepoint; AcceptAllChanges nếu original boolean true; return base count. Failure: rollback với None token; snapshot restore ngay cả cleanup fail; rethrow original hoặc AggregateException(original,cleanup). Không retain message objects trong ChangeTracker nên retry không stale message.

Test fixture mở SqliteConnection(:memory:) rồi EnsureCreated trên context có Outbox mapping; mọi DDL và data trong memory. Trigger abort Outbox kiểm tra rollback sau business write. SavedChangesInterceptor hủy token sau base async save kiểm tra rollback noncancelable. Cases còn lại như plan r1; không endpoint test, không DB thật. Audit được assert người tạo/sửa và timestamps, JSON được parse kiểm tra tên PK và không có secret.

## Review request

Xin host approve Skeleton r1 và explicit revalidate GroupPlan G01 r1 với skeleton1/plan1/spec3/overview3. Không có intended behavior change so với plan, không source/test/config scaffold đã tạo. Chờ execution authority sau gates.
