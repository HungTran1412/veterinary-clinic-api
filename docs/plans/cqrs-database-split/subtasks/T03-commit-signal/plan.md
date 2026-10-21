# Kế hoạch phát tín hiệu sau commit

runId: read-write-lag-20261021
taskId: T01-commit-signal
planRevision: 1
specRevision: 1
overviewRevision: 1

## Context và phạm vi

Spec1 được host duyệt T01-commit-signal-SPEC-01 trong 2026-10-21-tracking.md. Task kế thừa M. Chỉ ghi Data context/capture, interface, interceptor, OutboxCommitSignalTests và artifactRoot được giao. Infrastructure/DI/README/tests khác do host hoặc T02 sở hữu, không sửa. Dùng task-worker và backend-unit-test trong InvocationContext host-managed của dispatch; repo dùng xUnit/Moq/SQLite :memory:, comment Việt UTF-8 đầu hàm test. Không DB ngoài/migration/commit.

## Contract và luồng dữ liệu

Public interface do Data sở hữu có Notify/WaitAsync đúng Spec1. Write context constructor nhận optional singleton signal, tạo interceptor per-context khi enabled, AddInterceptors trong OnConfiguring. Capture nhận callback optional `Action<DbTransaction>`; sau capture thành công mark actual transaction, commit observer matching reference phát best-effort một lần. Nhánh lỗi vẫn recover như cũ; no-op/ack không callback; disabled hoặc thiếu signal tương thích caller cũ.

## Files và group

| Đường dẫn | Trách nhiệm | Owner |
| --- | --- | --- |
| VeterinaryClinic.Data/Synchronization/IReadModelSynchronizationSignal.cs | Contract public | T01 |
| VeterinaryClinic.Data/Systems/DataContext/VeterinaryClinicDataContext.cs | Optional injection, interceptor registration và callback | T01 |
| VeterinaryClinic.Data/Synchronization/ReadModelOutboxCapture.cs | Mark sau capture trước owned commit/sau external release | T01 |
| VeterinaryClinic.Data/Synchronization/ReadModelOutboxTransactionInterceptor.cs | Per-context marker và postcommit best-effort | T01 |
| VeterinaryClinic.Tests/Synchronization/OutboxCommitSignalTests.cs | Regression test trên relational isolated fixture | T01 |

Một group G01 theo groups/G01.md, hoàn tất hợp đồng và behavior producer cùng test. Không có predecessor/dependency. Sau Plan approval mới viết Markdown skeleton; skeleton current + G01 được host duyệt mới code.

## Regression cycle và kiểm chứng

1. Trong execution group được duyệt: thêm interface và optional constructor param nhưng chưa dùng signal, giữ mọi runtime cũ. Đây là contract-only scaffolding tối thiểu để test compile.
2. Viết `OwnedCommit_NotifiesAfterBusinessAndOutboxPersisted` cho bốn overload và bool accept, signal mock kiểm tra committed state sau Save. Gửi host command `dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --filter FullyQualifiedName~OutboxCommitSignalTests --verbosity quiet -m:1 -p:WarningLevel=0`; host chạy trước production logic. Red kỳ vọng fail Notify Times.Once = 0, không dùng compiler/environment failure làm chứng cứ.
3. Sau red host xác nhận, triển khai callback/interceptor/registration; thêm các case trong Spec1 theo rule thực sự khác nhau; host chạy slice test trên cho green.
4. Host chạy rộng `--filter FullyQualifiedName~Synchronization`, rồi toàn test/build khi tích hợp. Chỉ host chạy để tránh output race giữa workers.

Các case: owned sync/async+accept=false; external nhiều save và commit sync/async; external rollback/dispose không notify; lỗi insert Outbox hoặc constraint trước capture không notify và retry được; commit fault qua transaction interceptor trước commit không notify; cancel sau save không notify; external savepoint lỗi sau lần capture đầu giữ marker; marker cũ không ảnh hưởng transaction tiếp chỉ ack; disabled/no-signal/no-op/ack; notifier ném lỗi không báo ghi thất bại.

AC01 map timing + transaction tests; AC05 map disabled/ack/no-op và host integration DI; AC06 map suite/comment/diff review. SQLite không chứng minh SQL Server runtime hoặc commit trực tiếp underlying ADO transaction. Callback không chứa payload; polling bền vững vẫn thuộc T02.

## Rủi ro, recovery và handoff

Marker reference chỉ tác động transaction trùng; rollback clear marker, transaction mới không trùng kể cả caller dispose cũ. Notification failure bị chặn sau commit, không gọi recovery trên business đã commit. DataChangePlan N/A, không sửa database/schema. Handoff host sau G01/FinalResult: source diff, test commands/results do host cung cấp, revision nhất quán Spec1/Plan1/Skeleton1/G01. Done chỉ sau required tests Passed và host duyệt FinalResult. Chưa code; yêu cầu Plan1 và G01 Plan1 approval.
