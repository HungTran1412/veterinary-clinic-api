# Kế hoạch T02: Projection Outbox

runId: cqrs-db-split-20261020
taskId: T02-projection
planRevision: 1
specRevision: 2
overviewRevision: 3

## 1. Context

Spec r2 được host Approved và explicit revalidate overview3 trong host tracking ngày 20/10/2026 16:08 +07:00. T01 Spec r3 cùng approved ledger chốt public Data.Synchronization contract. InvocationContext vẫn host-managed/worker, decisionOwner=/root, workerId=/root/outbox_projection.

## 2. Constraints

Inherited L. Chỉ Infrastructure/Synchronization/**, Tests/Synchronization/Projection* và artifactRoot này. Không sửa test csproj, solution, Data, Shared, API, Business, migration, DB ngoài hoặc real config. Host giữ DI context/options bind. Chỉ SQLite in-memory làm test biệt lập.

## 3. Conventions

.NET8/EF8.0.2, namespace theo thư mục Infrastructure.Synchronization; dùng IOptions và DI scoped/hosted chuẩn. Tests xUnit/Moq, Assert, SQLite theo host. Áp dụng backend-unit-test behavior matrix; comment tiếng Việt đầu mỗi test. Không public endpoint mới.

## 4. Contracts

Public `ReadModelProjectionState(EntityType, KeyValues, Values)` chứa các dictionary tên EF property/object CLR; Values null là tombstone. `IReadModelProjectionStore` acquire lease/read pending/read authoritative/apply batch/ack. `ReadModelOutboxProcessor.ProcessBatchAsync(ct)` trả count ack; store exceptions không bị swallow trong processor. Worker catch lỗi từng lượt, delay theo options, cancel thoát. DI entrypoint `AddReadModelSynchronization()` không mở DB. T01 entity/options/DbSet giữ nguyên Spec r2. MARS disabled/target permissions do host validate/document.

## 5. Architecture and data flow

Worker -> scope -> processor -> exclusive source session lease -> pending -> authoritative row (detach stale tracked row) -> target batch transaction -> ack source -> release. Store giữ private read context instance, không ghi qua query scope/write SaveChanges vào replica. SQL helper chỉ metadata identifiers và provider type-mapped parameters; không nhận table/column từ JSON. UPDATE bỏ toàn bộ PK/identity; entity chỉ có composite keys thì tồn tại là no-op. SQLServer lock acquire/release là helper riêng để kiểm command offline; target identity ON được OFF trong finally dùng uncancelled cleanup. Nếu insert/commit lỗi: giữ exception gốc, rollback+dispose transaction trước retry OFF; cleanup lỗi thì close/dispose session, không che original error. Lock release finally bằng CancellationToken.None và close source connection cả khi release lỗi. Không trả connection còn identity/session state bẩn về pool.

## 6. Files

Exclusive worker owns `VeterinaryClinic.Infrastructure/Synchronization/{IReadModelProjectionStore,ReadModelOutboxProcessor,RelationalReadModelProjectionStore,ReadModelProjectionSql,SqlServerReadModelConsumerLock,ReadModelSynchronizationWorker,ReadModelSynchronizationServiceCollectionExtensions}.cs`. State record nằm cùng interface. Test files `VeterinaryClinic.Tests/Synchronization/{ProjectionProcessorTests,ProjectionStoreTests,ProjectionSqlTests,ProjectionWorkerTests}.cs`. Không file source ngoài danh sách.

## 7. Implementation groups

Một group G01 hoàn thành adapter+orchestration+worker+tests như một path tích hợp. Bên trong theo chiến lược host chọn: contract first -> processor red/green -> store/dialect/lock -> worker+tests -> scoped verify. [groups/G01.md](groups/G01.md). Không chia deployment hay tạo DDL.

## 8. Verification

AC03: SQLite current state/identity/composite/update/delete/replay/batch rollback; mock target failure/ack ordering. AC04: lock command offline, lease held until source ack/failure, pending lower ID, worker disabled/retry/cancel. AC07: `dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --filter FullyQualifiedName~Projection --verbosity minimal /m:1 /nr:false`; Infrastructure build nếu host yêu cầu/check mới cần. SQLServer external integration NotRun rõ ràng, static SQL review không thay thế runtime. Red đầu tiên processor missing behavior trả0 thay vì processed1; nếu environment chặn ghi đúng lý do và không claim red-green.

## 9. Integration

Host register original write/read context và IOptions trước AddReadModelSynchronization, configure read options QueryTrackingBehavior.NoTracking tương thích raw target SQL. T01 capture identity -> source pending -> target insert audit giữ nguyên -> ack không capture. Caller rollback không pending; commit thấp sau cao không checkpoint; replay sau ack lỗi đọc state mới. Host run full solution build/test sau worker scoped checks.

## 10. Risks and recovery

SQLServer session lease và IDENTITY_INSERT cần review explicit try/finally/cancellation; SQLite không chứng minh server semantics. Poison batch giữ pending/log error chứ không skip; DBA xử lý nguyên nhân. FK ngoài mapped schema/target writer khác không supported; ReadConnection cần DML+ALTER identity. DataChangePlan applicable=false ở execution; deployment baseline/rollback theo overview3. Schema thay đổi tương lai cần consumer revision tương ứng.

## 11. Decisions and approvals

Host đã chọn phase contract-first -> store/projector + worker tests -> verify. G01 chờ Plan+GroupPlan rồi Skeleton approval/revalidation. Không còn blocker nghiệp vụ; cần T01 source contract được tạo để build (Spec dependency đã Approved).

## 12. Completion

G01 result phải có actual check outputs, diff review, red/green thực tế hoặc giới hạn, acceptance mapping. Cuối cùng walkthrough/verify_result so Spec2+overview3 với actual implementation; host quyết FinalResult/overall Done. Không commit/push/deploy.
