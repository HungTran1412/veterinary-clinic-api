# Spec tích hợp đánh thức worker

runId: read-write-lag-20261021
taskId: Host-integration
specRevision: 1
overviewRevision: 1
sourceRefs: DatabaseServiceExtensions.AddClinicDatabases; ReadModelSynchronizationServiceCollectionExtensions.AddReadModelSynchronization; DatabaseServiceExtensionsTests; ReadModelIntegrationTests; README.md.

## Hợp đồng và hành vi

Đăng ký singleton IReadModelSynchronizationSignal → ReadModelSynchronizationSignal khi bật đồng bộ, trong extension đăng ký worker hiện có. Constructor write context nhận optional dependency; mọi scope và worker lấy cùng singleton. Không mở kết nối trong registration. Chế độ legacy tắt sync không đăng ký signal/worker. Không đổi connection/options/DTO hay ngưỡng cấu hình.

Tích hợp capture → signal → worker không thay durability: dữ liệu vẫn nằm trong Outbox và worker chỉ ack sau replica commit. Tín hiệu không thay thế message, không gửi key hay payload; chỉ tối ưu process đang chạy. Polling hiện tại tiếp tục giải quyết process restart, mất wakeup, consumer khác giữ lease hoặc commit từ instance khác. Sau lỗi processor vẫn nghỉ đủ chu kỳ, không retry ngay theo tín hiệu.

README cập nhật luồng wakeup sau commit, drain liên tiếp, polling khi rỗng và retry khi lỗi; khẳng định query vẫn eventual consistency. Không sửa file cấu hình mẫu đang có thay đổi trước task hoặc file appsettings thật.

## Test plan (backend-unit-test)

Stack: net8.0, xUnit 2.5.3, Moq 4.20.72, SQLite in-memory 8.0.2; dùng conventions hiện có và comment tiếng Việt đầu mỗi hàm test mới.

| Subject | Applicable Groups | Main Cases | Protected Rule |
| --- | --- | --- | --- |
| AddClinicDatabases/AddReadModelSynchronization | Happy, Boundary, Output, State, Side effect | Split resolve signal cùng object qua scope; read/write options không đổi, connection còn closed; disabled không signal/worker | Producer và worker cùng notifier, registration không chạm DB |
| CaptureAndProjection integration | Happy, State, Side effect, Transaction, Output | Source insert/update/delete phát wakeup sau commit; apply và ack không phát thêm wakeup, ID/audit giữ đúng | Không vòng lặp tự đánh thức do ack, không mất Outbox |

Authorization/permission, input format và nghiệp vụ bệnh viện không áp dụng: không thay endpoint/validation. Error/rollback/cancellation do T01/T02 có test chuyên biệt, không lặp lại ở host.

## Checklist review M

Yêu cầu và giới hạn đã rõ; contract không đổi API/schema; owner độc quyền đã có; đủ transaction, error/retry, signal timeout/cancellation và disabled; source hiện tại chứng minh cần singleton; tài liệu không hứa strong consistency; không dependency mới; không quyết định nghiệp vụ chặn.
