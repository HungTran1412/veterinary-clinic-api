# Spec tích hợp cấu hình CQRS

runId: cqrs-db-split-20261020
taskId: Host-integration
specRevision: 3
overviewRevision: 3
sourceRefs: Program.cs DI/Hangfire; VeterinaryClinicCallStoreHelperHandler.DatabaseConnectionString; RedisCacheService.GetOrCreate/Set; appsettings.json.example; README.

## Hành vi và hợp đồng

- Shared `DatabaseConnectionSettings`: resolve `WriteConnection`/`ReadConnection`, fallback `DefaultConnection` cho legacy. Không có kết nối thì lỗi cấu hình tiếng Việt; không in secret. Host registration parse SqlConnectionStringBuilder để nhận diện cùng server/database dù khác credentials/options; split bắt buộc sync enabled và cả hai connection explicit. Enabled true bắt buộc khác đích. Alias SQL Server không thể xác minh offline phải ghi trong tài liệu.
- Infrastructure/Configurations registration dùng write context với options capture, read context NoTracking, đăng ký worker/projector theo hợp đồng T01/T02. Không mở kết nối khi đăng ký/validate. Reject WriteConnection MARS=true khi sync enabled, vì caller transaction cần savepoint. Default sync false; sample dùng hai DB riêng và Enabled true, bỏ MARS=true. ReadConnection phục vụ query và projection nên cần DML/ALTER trên bảng business để giữ identity. File appsettings.json thật giữ nguyên.
- Program thay hai AddDbContext bằng registration; Hangfire dùng WriteConnection resolved. Không thay seeder hay chạy API.
- Stored procedure helper dùng ReadConnection cho medical record/dashboard; WriteConnection cho candidate doctor dùng trong command. Public method signatures giữ nguyên.
- CheckTokenQueryHandler dùng VeterinaryClinicDataContext để xác thực đọc trạng thái authoritative; giữ output/route, test tài khoản chỉ tồn tại source vẫn trả đúng thông tin. Query hồ sơ thông thường vẫn Read DB.
- Cache GetOrCreate và Set bỏ qua cache khi `ReadModelSynchronization:Enabled=true`; invalidation còn giữ như cũ. Không dùng flush Redis.
- Test project net8.0 xunit2.5.3, runner2.5.3, TestSdk17.8.0, Moq4.20.72, EF Sqlite8.0.2 đã có cache; không cần FluentAssertions. Tham chiếu Infrastructure và Business (Data transitive). Không khởi động API hay dùng SQL Server thật.
- README mô tả cấu hình, schema theo model T01, bootstrap baseline khi dừng ghi, vận hành retry, retention, poison message, rollback và giới hạn raw SQL/bulk. Schema mô tả trong docs, không tạo script/migration hay thực thi DDL.

## Acceptance và test matrix

| Method/subject | Applicable groups | Cases | Protected behavior |
| --- | --- | --- | --- |
| Connection resolver/registration | Validation, Happy, Boundary, Error, Output | legacy; explicit split; missing; same DB khác options; split disabled; enabled thiếu explicit; DI connections | Định tuyến không âm thầm dùng nhầm DB; cấu hình lỗi trước mở kết nối |
| RedisCacheService.GetOrCreate/Set | Happy, Side effect, Output, Error/State | split trả dữ liệu factory mới qua cả sync/async; bỏ qua get/set Redis; legacy cache vẫn đọc cache | Replica lag không bị lưu vào sliding cache; cache legacy không đổi |
| Stored procedure/Hangfire routing | Output/side effect | source review cả ba stored procedures và Hangfire connection | Command validation đọc authoritative write; query thực sự dùng read |

Permission/business rules/DB transaction không áp dụng cho resolver/cache; giao T01/T02 các ca transaction. Mọi test có comment tiếng Việt UTF-8. Build/test không cần DB thật.

## Spec Sentinel Report (host self-review)

Status: Approved for planning. Không TODO/TBD hay quyết định nghiệp vụ chưa giải quyết. Shared Logic: toàn bộ command/query dùng hai context được kiểm tra qua search; các stored procedure consumer gồm create appointment, medical record detail, dashboard revenue. Data/State: chỉ thêm model/feature flag, không DDL runtime; endpoint/DTO không đổi. Flow: invalid config fail fast, worker retry không đổi response commit business, query eventual consistency được người dùng chấp thuận. Security: không log connection hoặc payload secret; ReadConnection cần DML/ALTER để projector đồng bộ, query dùng NoTracking. Performance: cache bypass làm tăng tải read DB nhưng tránh lag kéo dài. Multi-tenant audit không áp dụng: không tenant/org trong model. Conventions: đúng tầng, không sửa migration, docs trong repo, tests backend. Giới hạn SQL Server integration được ghi rõ, không coi SQLite là bằng chứng IDENTITY_INSERT/application lock trên SQL Server.
