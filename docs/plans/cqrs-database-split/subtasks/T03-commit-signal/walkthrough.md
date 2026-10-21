# Walkthrough producer commit signal

runId: read-write-lag-20261021
taskId: T01-commit-signal
artifactRevision: 3
artifactRevisions: overview1/spec1/plan1/skeleton1/G01-plan1/G01-result1
checks: RED-SIGNAL Failed expected (exec23842); DIFF-WHITESPACE Passed; GREEN-SIGNAL Passed (exec60718); HOST-DEBUG-FULL-SUITE Passed (exec70817); HOST-RELEASE-BUILD Passed (exec22302); HOST-RELEASE-FULL-SUITE Passed (exec38653)
deviations: Không đổi intended behavior; thêm release savepoint failure case theo AC01, host xác nhận cùng scope.
limitations: Không SQL Server runtime/latency benchmark; EF observer không thấy commit trực tiếp underlying ADO transaction hoặc giao dịch context khác. Release build có 5 cảnh báo package đã có sẵn.

## Execution log

G01 C4 đọc ledger hiện tại trước code: Spec, Plan, Skeleton, GroupPlan đều Approved current revision; worker=/root/commit_signal, host=/root. Không dependencies/predecessor hoặc thiếu quyền. Chỉ ghi đúng năm source/test paths và artifactRoot.

Contract-only tạo interface + optional constructor param và sáu regression overload. Host chạy exec23842: test compile, sáu OwnedCommit case fail Moq Expected Notify Once but 0; total host slice 8failed/7s còn gồm worker/DI regression. Host cho tiếp production G01; không tự coi lỗi compile hay sandbox là regression.

Đã thêm interceptor per-context khi enabled+signal và callback mark actual DbTransaction: owned mark trước commit, external mark sau release savepoint. Notify matching transaction sau commit sync/async, clear trước Notify, catch notifier exception. Rollback matching clear; ack/no-op không mark. Không đổi SQL, schema, recover/audit hoặc accept semantics.

Đã thêm 30 test cases qua 11 methods theo matrix Spec1, dùng SQLite Shared Memory GUID riêng từng fixture, hai connection và Pooling=False. Notify callback đọc observer committed state và lưu exception, test assert ngoài callback tránh swallow che lỗi. Coverage: overload/acceptfalse, external multi-save, rollback/dispose và marker transaction kế, lỗi business/Outbox/commit/release, rollback savepoint giữ lần trước, cancellation, disabled/null signal, ack/no-op, notifier throw.

Đã chạy `git diff --check` trên context/capture: Passed, có cảnh báo LF→CRLF bình thường. Không tự build/test; host điều phối để tránh tranh chấp output. Host exec60718 exit0: combined slice67 Passed/0Failed,8.22s, build0errors, bao trọn 30 case mới cùng worker/signal/DI/integration/processor tests. Independent reviewer /root/latency_review GREEN, không actionable issue theo host. Full Debug suite exec70817 Passed132/Failed0/Skipped0,9s; T01 source không đổi sau test. Solution Debug build từng bị IIS Express/debugger giữ API DLL, host giữ process. Solution Release build exec22302 exit0,0errors/5packagewarnings có sẵn,47.89s. Final Release fullsuite exec38653 exit0,132Passed/0Failed/0Skipped,7s, bao toàn30producer cases, cùng source cuối không production/test changes.

ExecutionRef=T01-G01-exec-01. Đối chiếu Spec1/Plan1/Skeleton1 với source/test và green: marker matching transaction, postcommit observer, failed release không mark, multiple save gộp, rollback/dispose không rò signal sang transaction kế, ack/no-op không loop, notifier throw không báo lỗi sau commit; không discrepancy/intended behavior change. G01 Result1 đã được host duyệt T01-GROUP-RESULT-01 sau đọc actual diff/tests.

## Handoff

T02 consume interface public Notify/WaitAsync. Host inject cùng singleton vào optional write context parameter; không cần đăng ký interceptor vì OnConfiguring per-context đã thêm. DI/integration slice, Debug/Release fullsuite và solution Release build đã pass. Không migration/DB hệ thống/commit. Gửi FinalResult1 với walkthrough/verify_result revision3 cho host; host quyết định FinalResult approval và overall Done.
