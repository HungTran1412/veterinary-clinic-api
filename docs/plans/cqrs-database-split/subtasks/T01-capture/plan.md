# Kế hoạch T01: capture Outbox và kiểm thử

runId: cqrs-db-split-20261020
taskId: T01-capture
planRevision: 1
specRevision: 3
overviewRevision: 3

## 1. Context

Theo spec r3 đã được host approve/revalidate với overview r3. D05 bắt buộc tắt MARS do host startup validator. D07 host-only không ảnh hưởng capture. InvocationContext host-managed, role worker, decisionOwner /root.

## 2. Constraints

Chỉ ghi write DbContext, `VeterinaryClinic.Data/Synchronization/**`, `VeterinaryClinic.Tests/Synchronization/OutboxCaptureTests.cs` và artifact T01. Không migration, DB thật, API startup, commit hay package mới. Giữ constructor source-compatible; options thiếu đồng nghĩa disabled. Size L.

## 3. Conventions

EF Core 8.0.2, C# net8 nullable; audit giữ DateTime.Now và System/current user như hiện tại. xUnit 2.5.3 + SQLite in-memory theo test csproj host. Mỗi test có comment tiếng Việt đầu hàm. Áp dụng brainstorming/spec-reviewer ở bước thiết kế; writing-plans và backend-unit-test hướng dẫn test-first hiện tại.

## 4. Contracts

CLR/JSON/table giữ đúng spec r3: ReadModelSynchronizationOptions, ReadModelOutboxMessage và ReadModelOutboxMessages. SaveChanges()/Async(token) forward tới overload bool override đúng một lần; overload bool gọi trực tiếp `base.SaveChanges(bool)`/`base.SaveChangesAsync(bool,token)`, tránh vòng lặp virtual giữa overloads.

Capture helper nội bộ nhận context, acceptAllChangesOnSuccess và delegate persist(bool), hoặc async persist(bool,CancellationToken). Điều kiện trước: relational provider, không ambient transaction, caller transaction SupportsSavepoints. Kết quả: count do EF business save trả về, Outbox atomic; nếu true AcceptAllChanges sau thành công; false giữ EF states. Lỗi persistence/cancellation rollback và restore tracker; rollback cleanup thất bại phải giữ lỗi ban đầu cùng lỗi cleanup trong AggregateException, yêu cầu bỏ context vì trạng thái transaction không chắc chắn.

## 5. Architecture and data flow

Override bool → helper DetectChanges/snapshot → local transaction hoặc savepoint → delegate audit+base(false) → metadata PK thật → parameterized Outbox INSERT cùng connection/transaction → commit/release → optional AcceptAllChanges. Only-Outbox/no business delegate giữ accept boolean và không cần capture transaction. Không outbox entity kế thừa audit base.

Snapshot chứa EntityState, CurrentValues, OriginalValues, IsTemporary và IsModified để restore sau failure. Key payload chỉ đọc EF entry sau base save. Hard-delete entry vẫn có PK khi base(false). Shared helpers xử lý create command/message và tracking snapshot; sync/async dùng đúng IO tương ứng, async rollback bằng CancellationToken.None.

## 6. Files

| File | Thay đổi | Owner |
| --- | --- | --- |
| VeterinaryClinic.Data/Systems/DataContext/VeterinaryClinicDataContext.cs | Options optional, mapping/DbSet, audit common, 4 SaveChanges overloads | T01 |
| VeterinaryClinic.Data/Synchronization/ReadModelSynchronizationOptions.cs | Public options contract | T01 |
| VeterinaryClinic.Data/Synchronization/ReadModelOutboxMessage.cs | Public Outbox entity | T01 |
| VeterinaryClinic.Data/Synchronization/ReadModelOutboxCapture.cs | Atomic save và parameterized insertion | T01 |
| VeterinaryClinic.Data/Synchronization/OutboxEntrySnapshot.cs | Tracker recovery độc lập | T01 |
| VeterinaryClinic.Tests/Synchronization/OutboxCaptureTests.cs | SQLite fixture, fault injection, behavior tests | T01 |

## 7. Implementation groups

Host chọn contract-first → một G01 capture+tests → verification. [groups/G01.md](groups/G01.md) là nhóm duy nhất, gồm substeps đủ nhỏ: public contract/mapping; red tests; tracker recovery; transaction/capture; green và diff review. Không worker khác sửa các file này. T02 phụ thuộc hợp đồng CLR r1 đã chốt, có thể triển khai song song sau source contract xuất hiện.

## 8. Verification

AC02/AC07: `dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --filter FullyQualifiedName~OutboxCaptureTests -m:1 --verbosity minimal`. Trước implementation, tạo test disabled-compatible xác minh enabled capture không có Outbox bằng raw SELECT schema SQLite riêng để thấy red về behavior, không dùng compile failure làm red. Sau contracts/mapping có thể chạy toàn bộ tests trước helper để ghi thêm red đúng behavior.

Kịch bản: insert generated ID với bốn overload/audit; update; hard delete; composite key; keys-only user secret; no business/disabled; SaveChanges(false)+AcceptAllChanges; lỗi business; trigger abort Outbox rồi retry context; external tx rollback và multi-save commit; savepoint failure giữ save trước; cancellation trước và ngay sau business save qua interceptor. Assertion dữ liệu và trạng thái, không chỉ gọi mock. Ambient guard được test mà không mở DB ngoài; SQLServer MARS guard review + host config tests. SQLite không chứng minh provider SQLServer thực.

Build Data riêng hoặc test project serialized để tránh baseline MSBuild parallel failure; báo host trước khi test để không tranh build. Host integration chạy full suite sau cả workers.

## 9. Integration

T02 đọc/ack DbSet, Outbox only-save không phát message. Host register IOptions và source DI; legacy constructor vẫn hoạt động. Cross-module scenarios bám spec r3: identity/keys thật, external rollback, failure/retry không stale Outbox. Giao mapping/column/defaults để host viết deployment schema thủ công; không thực thi DDL ngoài SQLite test.

## 10. Risks and recovery

EF store-generated values khi SaveChanges(false) cần test tracker retry để phát hiện ID bị giữ sai. Savepoint dùng tên <32 ký tự để SQLServer hỗ trợ. Commit ambiguity do mất kết nối không thể hứa exactly-once; keys-only consumer idempotent. Nếu cleanup lỗi ném AggregateException và ghi rõ context không tái sử dụng. DataChangePlan applicable=false cho phiên này; deployment DBA do host tài liệu.

## 11. Decisions and approvals

Host ledger là authority. Spec r3 đã approve/revalidate overview3; kế hoạch r1 và GroupPlan r1 xin duyệt. Skeleton chưa viết, GroupPlan sẽ được host revalidate với skeleton r1 trước execute.

## 12. Completion

G01 result ghi red/green thực, diff, test count và limitations. Walkthrough/verify_result đối chiếu spec r3 với source, AC02/AC07 và C6. Host duyệt GroupResult/FinalResult rồi tích hợp full suite; worker không tự đánh dấu overall Done.
