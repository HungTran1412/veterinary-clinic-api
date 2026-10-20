# Walkthrough T02: đồng bộ Read DB

runId: cqrs-db-split-20261020
taskId: T02-projection
artifactRevision: 2
artifactRevisions: overview3/spec2/plan1/skeleton1/G01-plan1/G01-result2
checks: xem groups/G01.md và verify_result.md
deviations: Không đổi intended behavior; host tập trung final tests tại root
limitations: SQL Server thật chưa chạy; DBA baseline và schema ngoài phiên này

Processor giữ application lock source xuyên đọc pending/live source, commit replica rồi ack. Store không checkpoint ID, nên pending thấp commit trễ không bị bỏ qua. Keys JSON phải đúng toàn bộ PK trong EF business model; Outbox và arbitrary identifiers bị reject. Mọi giá trị đi qua relational type-mapped parameters, identifiers từ model và provider delimiter.

Replica là instance riêng VeterinaryClinicReadDataContext. Apply batch trong transaction, giữ identity và audit; xóa khi source đã mất. Existing composite-only row là no-op, UPDATE không thay primary key. Lỗi/cancel rollback, không ack; sau target commit nhưng ack lỗi, lượt sau đọc lại nguồn rồi replay idempotent. SQLite test fixture chỉ hai database :memory: biệt lập.

SQL Server acquire/release fixed sp_getapplock Session/Exclusive/timeout0, release với CancellationToken.None. IDENTITY_INSERT OFF được retry sau rollback/dispose khi operation lỗi. Cleanup uncertainty bỏ pool trước close bằng SqlConnection.ClearPool, giữ original exception. Không clear pool happy path. SQLServer commands/lock lifecycle có seam kiểm offline/mocks; không khẳng định integration SQLServer.

Worker mặc định disabled, không tạo database scope khi tắt. Bật thì scope mới mỗi poll, log lỗi và retry; cancellation thoát và trả lease. AddReadModelSynchronization chỉ đăng ký scoped store/processor và hosted worker; host sở hữu connections/options validation. ReadConnection shared query/projector cần SELECT+DML+ALTER identity, WriteConnection MARS=false theo host spec.

Tests: processor order/no-lock/empty/failure/poison/cancel/retry latest tombstone; store identity/audit/current state/composite/delete/rollback/invalid JSON/entity/lower pending ID/ack không feedback; SQLServer parameter/immutable PK/identity commands và mocked application-lock cleanup; worker disabled/error retry/shutdown. Mỗi test có comment tiếng Việt UTF8 đầu hàm.

Actual RED focused processor: outside sandbox session62160 exit1, 1 Failed/0 Passed trước implementation (baseline return0). Sandbox attempt compile pass nhưng testhost socket timeout90s, không tính behavior evidence. Final GREEN do host chạy `dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --verbosity quiet -m:1 -p:WarningLevel=0` ngoài sandbox: session45574 exit0, Passed88/Failed0/Skipped0 trong7s, gồm tất cả Projection* và test tích hợp thật capture→store→processor→ack cho insert/update/delete. Build solution host báo exit0/0errors; warnings package/audit có sẵn. executionRef HOST-FULL-TEST-20261020-45574.
