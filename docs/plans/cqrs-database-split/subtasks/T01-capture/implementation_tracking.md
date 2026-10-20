# Theo dõi T01-capture

runId: cqrs-db-split-20261020
taskId: T01-capture
workerId: /root/outbox_capture
localProgress: AwaitingApproval GroupResult/FinalResult
approvalRefs: host ledger Spec3 (revalidate overview3), Plan1, Skeleton1, GroupPlan1 (skeleton1), execute 2026-10-20T16:13:13+07:00
blockers: Không; host full suite đã pass

## Kiểm tra readiness

C4 đã đọc ledger trước source edit. Chỉ sửa đúng writeScope; host sở hữu csproj/DI/cấu hình, T02 sở hữu Infrastructure. Không migration/DB ngoài/commit. Hợp đồng CLR giữ spec3.

## G01

- Public contracts, DbSet và mapping đã tạo, compile pass trong bước Red.
- RED thực: `dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --filter FullyQualifiedName~OutboxCaptureTests -m:1 --verbosity minimal`, exec session63535 exit1. `EnabledSave_PersistsGeneratedKeyWithoutBusinessPayload` FAIL tại line27 `Assert.Single(): collection was empty`; business count và generated Id đã pass trước đó. Đây là missing behavior, không compile/environment failure.
- Capture core sync/async, metadata key payload, audit đủ overload, tracker recovery và expanded tests đã viết sau Red.
- Test process63535 finished, trả slot T02 chạy processor red trước focused green của T01. Lần test dùng require_escalated để VSTest loopback hoạt động, không external DB.
- GREEN thực: command quiet filter OutboxCaptureTests, session66043 exit0, Passed26/Failed0/Skipped0, duration2s. Process confirmed completed trước trả slot.
- Reviewer host phát hiện thiếu guard test SupportsSavepoints=false; thêm hai Theory cases sync/async bằng IRelationalTransactionFactory/RelationalTransaction SQLite override SupportsSavepoints=false. Không mở SQL Server. Hai case mới chờ host full suite; không dispatch test bổ sung theo điều phối host.
- `git diff --check` pass; tên file đúng scope; comment tiếng Việt đầu mọi test; exceptions mới tiếng Việt UTF-8.
- HOST FULL GREEN: session45574 completed exit0, `dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --verbosity quiet -m:1 -p:WarningLevel=0`, Passed88 Failed0 Skipped0 duration7s. Bao gồm hai no-savepoint guard cases và host real capture→projection→ack integration. Host review code/test guard không còn YELLOW. executionRef=HOST-FULL-TEST-20261020-45574.
- Host build solution pass exit0 với -m:1 --no-restore -p:WarningLevel=0, 0 errors, package/audit warnings có sẵn. Không test thêm sau host integration pass.
