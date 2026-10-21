# Theo dõi worker

runId: read-write-lag-20261021
taskId: T02-worker-wakeup
localProgress: G01 AwaitingApproval (GroupResult2 / FinalResult3)
approvalRefs: T02-worker-wakeup-SPEC-01; T02-worker-wakeup-Plan-01; T02-worker-wakeup-Skeleton-CURRENT-01; T02-worker-wakeup-GroupPlan-CURRENT-01 tại ../../2026-10-21-tracking.md
blockers: Không còn check bắt buộc Failed/NotRun; chờ host GroupResult và FinalResult approval.

| Group | Plan revision | Result revision | Local state | Host decision reference | Next action |
| --- | --- | --- | --- | --- | --- |
| G01 | 1 | 2 | AwaitingApproval | GroupPlan-CURRENT-01 | Host review GroupResult 2 và FinalResult 3 |

Đã kiểm identity/ownership/current gates C4 trước test edit và trước production edit. Worker không tự chạy build/test để tránh tranh chấp output với T01/host. Host xác nhận RED-DRAIN từ exec23842 exit1: Worker_WhenPendingRemains_DrainsPartialBatchesWithoutPollingDelay thất bại TimeoutException tại WaitAsync sau 5 giây khi worker cũ dừng sau batch đầu (poll3600). Đây là thất bại behavior dự kiến, không phải compile failure. Combined red exec12077: 8 Failed, 0 Passed gồm drain, missing notify và DI.

Host đã xác nhận red và cho phép runtime G01. Đã hiện thực Channel signal và worker; thêm 7 tests mới vào ProjectionWorkerTests (tổng10) và 6 signal tests. Signal callback/scope tests dùng mock store, không DB. Retry flood dùng poll3600 để tránh assert phụ thuộc thời gian tải máy; test retry cũ và fallback poll1 vẫn xác nhận thử lại sau timeout. Đây là lựa chọn test trong acceptance hiện có, không đổi intended behavior.

Host GREEN exec60718 exit0: combined67 Passed/0 Failed, 8.22 giây, build0 errors; bao gồm10 worker+6 signal tests. Independent review GREEN theo host. Sau đó host cho phép đổi fallback assertion attempts==2 thành >=2 để tránh phụ thuộc scheduling khi polling1; đã patch và báo source/test stable. Acceptance không đổi (vẫn cần ít nhất hai lượt polling, test lease busy bảo vệ không spin). Host chạy finalbuild/fullsuite binary mới; worker không tự chạy tests.

git diff --check scoped sau patch exit 0 (warning LF→CRLF không phải lỗi whitespace, chunk d287ef). Strict UTF-8 decode bốn file source/tests và không có replacement character đều đạt (chunk 513839). Kiểm tra static comment cho đủ 16 tests worker/signal: mỗi test có comment tiếng Việt đầu hàm. Code-reviewer advisory scoped source/tests: warning fallback scheduling đã sửa, không còn RED/YELLOW.

Host báo final current checks: exec22302 Release solution build exit 0, 0 errors, 5 package warnings có sẵn trong 47,89 giây; exec38653 Release full backend suite exit 0, 132 Passed/0 Failed/0 Skipped trong 7 giây. Binary đã bao gồm assertion >=2 cuối. Host dùng Release để tránh Debug DLL bị IIS khóa, không dừng IIS. GroupResult 2 và verify/walkthrough revision 3 đã cập nhật Passed, sẵn sàng host C3 review. Source/tests không đổi sau final evidence.
