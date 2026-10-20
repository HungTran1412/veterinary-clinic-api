# Walkthrough T01-capture

runId: cqrs-db-split-20261020
taskId: T01-capture
artifactRevision: 2
artifactRevisions: spec3, plan1, skeleton1, groupPlan1, overview3 (host revalidated)
checks: RED actual; GREEN26 actual; host full suite88 pass, gồm hai guard cases và integrated capture→projection→ack
deviations: Không thay intended behavior
limitations: SQL Server DB integration không thực thi theo ràng buộc người dùng

## Kết quả source

Write DbContext có optional IOptions ReadModelSynchronizationOptions và mapped DbSet ReadModelOutboxMessages. Mapping dùng provider defaults, identity và pending index. Capture mặc định tắt, constructor cũ vẫn hoạt động; không startup DDL hay migration.

Bốn overload SaveChanges đi qua audit một lần, base bool overload một lần. Enabled capture: snapshot changed entries trước audit; owned transaction hoặc caller savepoint; base(false) lấy generated keys; INSERT Outbox parameterized cùng connection/transaction; commit/release rồi optional AcceptAllChanges. Payload chỉ EF entity name và primary-key JSON; Outbox không tự capture. Snapshot restore giữ trạng thái/current/original/temp/modified sau rollback để retry cùng context. Async cleanup rollback dùng None token. Ambient transaction hoặc caller không savepoint bị từ chối trước business write.

## Bằng chứng

Regression Red ghi đúng missing behavior: business lưu được nhưng Outbox empty. Sau implementation 26 case focused pass, bao phủ bốn overload, audit, insert/update/hard-delete, composite key, SaveChanges(false) với Added/Modified/Deleted, keys-only credentials, disabled/no-op/ack, transaction rollback/nhiều saves/savepoint giữ lần trước, business/Outbox failure và retry, pre/post-business cancellation, ambient guard. Hai case no-savepoint guard được bổ sung sau review và đã pass trong host full suite88 (session45574 exit0, 0 failures, duration7s), cùng real capture→projection→ack integration. Tổng T01 có28 case.

Không thay source ngoài ownership, database thật, migrations/snapshot, cấu hình thật hay Git commit. C6 contract không đổi so với spec3; new guard tests chỉ lấp verification gap, không thay hành vi.

executionRef: HOST-FULL-TEST-20261020-45574. Host solution build pass 0 errors; không cần source sửa thêm. Chờ host GroupResult/FinalResult approval.
