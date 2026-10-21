# Kế hoạch tích hợp wakeup

runId: read-write-lag-20261021
taskId: Host-integration
planRevision: 1
specRevision: 1
overviewRevision: 1

## Chiến lược

Host chọn Contract First + Test First theo task-lead: chốt notifier contract, test worker/capture red trước implementation tương ứng, sau đó tích hợp DI và pipeline rồi chạy bộ test. Không tách sprint vì đây là thay đổi backend có ranh giới rõ và không cần triển khai hạ tầng. Không đổi thứ tự/acceptance đã được người dùng yêu cầu sửa.

## Contracts và quy ước

IReadModelSynchronizationSignal (Data namespace) có Notify(): void, WaitAsync(TimeSpan timeout, CancellationToken): Task. T01 owns interface/producer, T02 owns Channel/worker, host owns DI. Singleton cho mọi request scope; constructor optional giữ source compatibility. Tín hiệu không bền vững; Outbox và polling chịu trách nhiệm hội tụ. Dùng net8/xUnit/Moq/SQLite :memory:, tên PascalCase, comment tiếng Việt UTF-8; không package mới.

## Artifact registry và dependency

| File | Intent | Owner |
| --- | --- | --- |
| Infrastructure/Synchronization/ReadModelSynchronizationServiceCollectionExtensions.cs | Đăng ký singleton signal | Host |
| Tests/Configuration/DatabaseServiceExtensionsTests.cs | Split singleton, disabled không signal, không mở connection | Host |
| Tests/Configuration/ReadModelIntegrationTests.cs | Capture phát wakeup nhưng projection/ack không feedback | Host |
| README.md | Luồng và giới hạn wakeup | Host |
| integration-spec/plan/skeleton, tracking, result ngày 21/10 | Bằng chứng thiết kế và kiểm chứng | Host |

Đường dẫn source dưới prefix VeterinaryClinic.; không sửa DatabaseServiceExtensions nếu optional DI constructor đủ. Host chỉ bắt đầu edit source/tests sau T01/T02 skeleton approvals và interface có trong source để tránh test fail vì compile thiếu contract.

## Nhóm G01 — Wiring, integration và tài liệu

groupId: G01; groupPlanRevision: 1; groupResultRevision: 0; planRevision: 1; skeletonRevision: 1 (phải revalidate sau khi skeleton được duyệt).

1. Thêm test DI singleton/disabled và pipeline notification, chạy slice để quan sát red do registration thiếu; không tính compile/setup failure là red.
2. Đăng ký singleton tại extension hiện có, không đổi cấu hình hoặc kết nối.
3. Chạy test slice; cập nhật README từ luồng polling từng batch sang commit wakeup + drain, retry/fallback.
4. Khi worker groups hoàn tất, build solution và full tests tuần tự, review diff gồm transaction/cancellation/DI và check UTF-8/whitespace/config preservation.

Acceptance: AC05/06; integration-spec1 và overview1. Không test UI. Không khởi động host API, chỉ resolve DI không mở DB. SQLite dùng in-memory biệt lập, không connection thật. Không đổi schema/migration.

## Edge cases và kiểm chứng

Disabled → không signal/worker; nhiều scopes → cùng signal; capture owned/external commit → đánh thức; ack-only → không wakeup; lost/foreign-process signal → polling; failure → backoff. T01/T02 bảo vệ chi tiết transaction và channel, host kiểm tra pipeline và ownership. Commands: dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --filter FullyQualifiedName~ReadModel; dotnet build VeterinaryClinic.Solution.sln --no-restore -m:1; dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore -m:1. Thực tế command có thể thêm verbosity/warnings nhằm giảm output, không thay phạm vi.

Không claim SQL Server integration hoặc cải thiện p95 thực tế. Nếu build/test bị sandbox/network chặn, dùng escalation đúng chính sách, không đổi acceptance thành Passed.
