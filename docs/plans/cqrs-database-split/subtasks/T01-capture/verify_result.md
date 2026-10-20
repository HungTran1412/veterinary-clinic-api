# Verification T01-capture

runId: cqrs-db-split-20261020
taskId: T01-capture
artifactRevision: 2
artifactRevisions: spec3, plan1, skeleton1, groupPlan1, overview3
deliveryState: AwaitingApproval
limitations: SQL Server integration NotRun theo ràng buộc người dùng; không còn required check pending

| checkId | commandOrProcedure | repoRoot | result | evidenceRef | reason | relatedAcceptanceIds |
| --- | --- | --- | --- | --- | --- | --- |
| CAP-RED | dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --filter FullyQualifiedName~OutboxCaptureTests -m:1 --verbosity minimal | D:/.NET/veterinary-clinic-api | Failed (expected Red) | exec session63535 exit1, OutboxCaptureTests line27 | Business count/identity có nhưng Outbox empty trước implementation | AC02, AC07 |
| CAP-GREEN | dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --filter FullyQualifiedName~OutboxCaptureTests --verbosity quiet -m:1 -p:WarningLevel=0 | D:/.NET/veterinary-clinic-api | Passed | exec session66043 exit0, Passed26 Failed0 Skipped0 duration2s | Meaningful SQLite relational behavior tests, không DB ngoài | AC02, AC07 |
| CAP-GUARD | CallerTransactionWithoutSavepoints_IsRejectedBeforeBusinessWrites(false/true) qua host full suite | D:/.NET/veterinary-clinic-api | Passed | HOST-FULL-TEST-20261020-45574, Passed88 Failed0 Skipped0 | SQLite transaction factory không hỗ trợ savepoint bị reject trước business write | AC02, AC07 |
| CAP-INTEGRATION | dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --verbosity quiet -m:1 -p:WarningLevel=0 | D:/.NET/veterinary-clinic-api | Passed | host exec session45574 completed exit0, Passed88 Failed0 Skipped0 duration7s | Gồm toàn bộ28 T01 cases, real capture→projection→ack và host/T02 regression tests | AC02, AC07 |
| CAP-DIFF | git diff --check; đọc source/test và phạm vi file | D:/.NET/veterinary-clinic-api | Passed | worker tool output và source diff | Không whitespace errors; comment và exceptions UTF-8; không migration/DB thật/commit | AC02, AC07 |
| CAP-SQLSERVER | Chạy SQL Server integration | D:/.NET/veterinary-clinic-api | NotRun | overview constraints | Người dùng không cho thao tác database ngoài; SQL/transaction dùng API relational + parameterized command được review | AC02 |

## AC và C6 consistency

AC02 có chứng cứ green26 cho atomic identity/composite/delete/failure/retry/caller tx/false accept/cancel; cả28 T01 cases gồm no-savepoint guards đã pass host full suite88. AC07: solution build và focused/full suite pass, mỗi hàm test có comment tiếng Việt. Host đã review guard code/tests không còn YELLOW. Không còn required Failed/NotRun ngoài SQL Server integration được overview loại khỏi phiên do ràng buộc người dùng.

So sánh spec3 (overview3 host explicit revalidation) với source/helper/tests hiện hành: phù hợp CLR contract, audit, atomic commit/rollback, supported transaction và payload keys-only; không acceptance change. DataChangePlan applicable=false trong phiên. Mapping được host dùng cho deployment prerequisite, không tự chạy DDL ngoài SQLite in-memory.

FinalResult request: runId=cqrs-db-split-20261020; taskId=T01-capture; workerId=/root/outbox_capture; artifactRevision=2; artifactRevisions={spec3,plan1,skeleton1,groupPlan1,groupResult2,overview3}; acceptanceStatus={AC02:Passed,AC07:Passed}; integrationChecks=HOST-FULL-TEST-20261020-45574; deliveryState=AwaitingApproval; limitations=SQL Server DB integration không thực thi, commit ambiguity theo EF/transaction chuẩn, raw SQL/bulk ngoài EF cần cơ chế riêng như tài liệu host. Xin host GroupResult2 và FinalResult2 approval.
