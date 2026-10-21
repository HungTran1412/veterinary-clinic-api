# Kiểm chứng worker wakeup

runId: read-write-lag-20261021
taskId: T02-worker-wakeup
artifactRevision: 3
artifactRevisions: {overviewRevision: 1, specRevision: 1, planRevision: 1, skeletonRevision: 1, groupPlanRevision: 1, groupResultRevision: 2}
repoRoot: D:\.NET\veterinary-clinic-api

## CheckResult

| checkId | commandOrProcedure | result | evidenceRef / reason | relatedAcceptanceIds |
| --- | --- | --- | --- | --- |
| RED-DRAIN | dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --filter FullyQualifiedName~Worker_WhenPendingRemains_DrainsPartialBatchesWithoutPollingDelay | Failed (dự kiến) | Host exec23842 exit1; TimeoutException sau5 giây ở WaitAsync trên worker cũ polling3600, không chờ batch tiếp | AC02 |
| GREEN-WORKER-SIGNAL | Host chạy combined worker/signal/capture/DI slice sau implementation | Passed | exec60718 exit0, 67 Passed/0 Failed trong8.22 giây, build0 errors, gồm10 worker+6 signal tests; trước adjustment assertion fallback | AC02/03/04/06 |
| STATIC-DIFF | git diff --check scoped worker/tests sau adjustment | Passed | chunkd287ef exit0, chỉ warning LF→CRLF | AC06 |
| STATIC-COMMENTS-UTF8 | Đọc/rg test methods+comments, decode strict UTF-8 bốn file và kiểm replacement character | Passed | 10 worker +6 signal tests đều comment; chunk513839 UTF8 OK4 file | AC06 |
| STATIC-REVIEW | Code-reviewer scoped4 paths + interface/processor dependency | Passed | Advisory GREEN sau sửa scheduling warning; independent review GREEN theo host; không thay host approval | AC02/03/04/06 |
| HOST-CURRENT-BUILD | Release solution build sau assertion fallback >=2 | Passed | Host exec22302 exit 0; 0 errors, 5 package warnings có sẵn; 47,89 giây | AC06 |
| HOST-CURRENT-FINAL | Release full backend suite sau assertion fallback >=2 | Passed | Host exec38653 exit 0; 132 Passed/0 Failed/0 Skipped; 7 giây; binary chứa adjustment cuối | AC02/03/04/05/06 |

## Đối chiếu spec và acceptance

| Acceptance | Implementation/test coverage | Trạng thái |
| --- | --- | --- |
| AC02 | processed>0 continue sau scope disposal; red drain original giữ nguyên hai partial batch+empty với poll3600 | Passed final Release suite |
| AC03 | Bounded signal giữ wakeup trước/sau wait; coalesce concurrent producer; timeout/cancel không mất signal tiếp; worker idle notify và fallback poll1 | Passed final Release suite, gồm assertion fallback >=2 chống scheduling flake |
| AC04 | Error Delay độc lập signal, busy =0 wait; source-read/idle/backoff cancellation và scope test; processor/lease logic giữ nguyên | Passed final Release suite |
| AC06 phần worker | Test comments/UTF8 và diff/review static đạt, Release build 0 errors, backend 132 pass | Passed; chờ host C3 approval |

Spec 1, plan 1, skeleton 1, overview 1 và source/test hiện tại khớp behavior đã duyệt qua static review và final Release evidence. Supporting test-only fix sau green không thay intended behavior: polling phải thử ít nhất hai lượt, không yêu cầu đúng hai lượt khi thread bị schedule trễ. Không path ownership conflict, không có required check Failed/NotRun còn mở. GroupResult 2 và FinalResult revision 3 sẵn sàng host review; worker không tự phê duyệt.

## FinalResult

runId: read-write-lag-20261021; taskId: T02-worker-wakeup; artifactRevision: 3; executionRef: G01-worker-wakeup-20261021-02.

acceptanceStatus: AC02/AC03/AC04 và phần worker AC06 Passed, evidence theo bảng trên. integrationChecks: final Release solution build Passed; full backend suite 132 Passed/0 Failed/0 Skipped, gồm test wiring/capture/worker của host. deliveryState: implementation và verification hoàn tất, AwaitingApproval từ host cho GroupResult 2 và FinalResult 3. limitations: SQL Server/latency thật chưa đo theo phạm vi; 5 package warnings có sẵn, build 0 errors; không migration/DB/API run/commit. Không source/test changes sau final check.

## Giới hạn

Không thay đổi DB/schema hoặc chạy API/seeder. Mock store chứng minh orchestration/lifecycle, không chứng minh SQL Server sp_getapplock/IDENTITY_INSERT hoặc đo latency hệ thống thật. Signal chỉ đánh thức cùng process, polling hiện có vẫn bảo đảm thử lại khi restart, tín hiệu thất lạc hoặc writer khác instance.
