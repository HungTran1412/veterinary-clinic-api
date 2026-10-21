# Thay đổi worker và signal

runId: read-write-lag-20261021
taskId: T02-worker-wakeup
artifactRevision: 3
artifactRevisions: {overviewRevision: 1, specRevision: 1, planRevision: 1, skeletonRevision: 1, groupPlanRevision: 1, groupResultRevision: 2}

## Thay đổi

- ReadModelSynchronizationSignal implement contract T01 qua Channel bounded1, nhiều producer và một consumer. Notify TryWrite không chặn; signal gộp và giữ được khi phát trước Wait. Wait trực tiếp read bằng linked token có timeout, không tạo reader mồ côi sau timeout/cancel; token đã hủy không tiêu thụ buffered signal.
- ReadModelSynchronizationWorker giữ ctor3 tương thích nhờ optional signal cuối. Worker xử lý tiếp ngay khi processed>0 kể cả partial batch; =0 chờ signal hoặc timeout polling. Scope đóng trước batch mới/chờ. Lỗi luôn chờ Delay polling độc lập signal; stoppingToken kết thúc graceful.
- ProjectionWorkerTests có 7 tests mới và giữ3 tests cũ; ReadModelSynchronizationSignalTests có6 tests mới. Bao phủ drain, idle wake, poll fallback, busy lease, failure signal flood, scope disposal, stop idle, signal coalescing và timeout/cancel.

## Kiểm chứng hiện có

RED-DRAIN: host exec23842 exit1, regression timeout sau5 giây trên worker cũ polling3600, đúng nguyên nhân chờ giữa batch. GREEN: host exec60718 exit0 combined67 Passed/0 Failed trong8.22 giây, gồm10 worker+6 signal tests; build0 errors. Original red regression giữ nguyên và đã green sau worker change.

Sau GREEN, đã sửa riêng assertion fallback attempts==2 thành >=2 theo host cho phép: thread test schedule trễ có thể làm worker chạy hơn hai lượt dù đúng behavior. Vẫn kiểm chứng tối thiểu hai lượt và có test busy riêng bảo vệ không spin. Final current evidence: exec22302 Release solution build exit 0, 0 errors, 5 package warnings có sẵn, 47,89 giây; exec38653 Release backend suite exit 0, 132 Passed/0 Failed/0 Skipped, 7 giây, binary gồm adjustment cuối. Host dùng Release để tránh Debug DLL bị IIS giữ; không dừng IIS. git diff --check sau adjustment exit 0, strict UTF-8/no replacement character bốn file source/tests đạt. Worker không tự chạy build/test hay DB.

## Review advisory

Code-reviewer Phase 0 tái sử dụng context hiện có. Scope 4 source/test paths được giao và direct dependency interface/processor. Verdict GREEN sau sửa warning fallback scheduling; không còn RED/YELLOW. Host cũng báo independent review GREEN cùng output green và đọc lại implementation/tests. Channel bounded và synchronous continuation tắt giữ producer nhẹ; cancellation được quan sát trước dispose CTS; worker không skip scope disposal hoặc retry backoff. Tests giữ subject thật và mock store, comment tiếng Việt UTF-8 đầy đủ. Review advisory không thay host C3 approval.

## Giới hạn và deviations

Không đổi spec behavior hoặc writeScope. Không đổi SQL/processor/lock/config/database, API, migration hay commit. Tín hiệu chỉ trong process; polling là đường dự phòng. Không benchmark SQL Server/latency thật, không cam kết strong consistency. Bộ test tích hợp cùng singleton producer/worker nằm trong Release backend suite đã pass; host quyết định phê duyệt delivery cuối.
