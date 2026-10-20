# Đặc tả T02: Projection Outbox sang Read DB

runId: cqrs-db-split-20261020
taskId: T02-projection
workerId: /root/outbox_projection
specRevision: 2
overviewRevision: 2
sourceRefs: `VeterinaryClinic.Data/Systems/DataContext/VeterinaryClinicDataContext.cs` SaveChangesAsync/OnModelCreating; `VeterinaryClinicReadDataContext.cs` OnModelCreating; `Systems/Context/VcDoctorSpecializations.cs` khóa ghép; `VeterinaryClinic.Infrastructure/VeterinaryClinic.Infrastructure.csproj`; hợp đồng T01 trao đổi ngày 20/10/2026.

## Yêu cầu và giới hạn

AC03: đọc pending theo batch, đọc lại trạng thái authoritative, upsert/delete replica giữ nguyên primary key, transaction target hoàn tất trước ack. AC04: serialize consumer xuyên API instances, không checkpoint theo ID, retry/restart/cancellation an toàn và không crash host khi DB lỗi. AC07: unit test hành vi, SQLite memory và mock; comment tiếng Việt UTF-8 đầu mỗi test.

Chỉ sửa Infrastructure/Synchronization, Tests/Synchronization/Projection* và tài liệu subtask này. Không migration, schema script tự chạy, real appsettings, DB ngoài, API startup, bootstrap tự động, commit. Không thay endpoint/API DTO. Dữ liệu cũ do DBA baseline copy trong thời gian dừng ghi theo overview.

## Hiện trạng và phương án

Hai context hiện có dùng cùng business entity; read context không audit SaveChanges. Infrastructure tham chiếu Business rồi Data, dùng .NET 8/EF Core 8.0.2. Model có 16 entity, khóa ghép DoctorId/SpecializationId; không khai báo relationship/FK trong model hiện tại. Chưa có worker/consumer/test stack trong baseline.

Chọn tái sử dụng model read context, tạo một instance replica riêng trong store qua DbContextOptions<VeterinaryClinicReadDataContext>; không clone model, không lưu replica qua write context. Hai lựa chọn khác: generic EF Add/Update gặp identity SQL Server; payload snapshot có thể ghi đè state mới khi commit ID lệch thứ tự. Vì vậy dùng metadata business + tham số DB để viết replica, lấy live state từ Write DB.

## Contract với T01 và host

Namespace Data.Synchronization. `ReadModelOutboxMessage`: long Id identity; string EntityType (EF metadata Name); string KeyValuesJson (JSON object tên PK CLR/EF -> primitive); DateTime CreatedAtUtc; DateTime? ProcessedAtUtc. Bảng `vcReadModelOutbox`, PascalCase columns. Write context expose `ReadModelOutboxMessages`, exclude chính Outbox khỏi capture/projection. Options `ReadModelSynchronizationOptions`: SectionName, Enabled=false, PollIntervalSeconds=5, BatchSize=100. T01 sở hữu entity/options/map; host sở hữu bind/validate options và kết nối khác DB.

DI entrypoint `IServiceCollection.AddReadModelSynchronization()` đăng ký scoped `IReadModelProjectionStore -> RelationalReadModelProjectionStore`, scoped `ReadModelOutboxProcessor`, hosted `ReadModelSynchronizationWorker`. Host chỉ gọi sau đăng ký contexts/options. Không thêm cấu hình ngoài 3 options đã chốt.

Store contract: AcquireConsumerLockAsync(ct) -> IAsyncDisposable? (null nếu consumer khác đang chạy); ReadPendingAsync(batchSize,ct); ReadCurrentStateAsync(message,ct) -> state metadata/keys/scalar values hoặc tombstone; ApplyAsync(states,ct) -> chỉ return sau target transaction commit; MarkProcessedAsync(messages,ct). Processor giữ khóa xuyên toàn bộ read-source/apply/ack, trả số message ack thành công. Store disposable sở hữu replica context riêng; source context do scope DI sở hữu.

## Luồng và tính đúng

1. Worker enabled tạo scope mỗi vòng; disabled thoát không resolve context/mở DB. Retry sau PollIntervalSeconds khi lỗi; cancellation dừng êm. Không tạo schema, không EnsureCreated trong production.
2. Source SQL Server connection mở và gọi sp_getapplock Session/Exclusive, resource cố định `VeterinaryClinic.ReadModelSynchronization`, LockTimeout=0. Return -1 -> không có việc vòng này; mã âm khác -> throw, không đọc/apply. Khóa nằm trong source DB, các API instance cùng source serialize. Release trong finally bằng token không bị cancel và đóng connection do mình mở; giữ khóa tới ack thành công/lỗi.
3. Query `ProcessedAtUtc == null`, OrderBy(Id), Take(BatchSize). Không high-water mark; pending thấp commit trễ vẫn được đọc vòng sau. Payload được kiểm tra trọn vẹn trước mọi ghi SQL target: entity chỉ thuộc business model read/write, không Outbox; keys đúng toàn bộ PK, không null/thừa/thiếu, JSON primitive đúng CLR type. SQL identifiers chỉ từ EF metadata, quote qua provider; mọi giá trị parameterized.
4. Đọc source theo key, trích scalar properties, detach entity để lần đọc sau không lấy stale tracked state. Entity không còn -> tombstone delete. Không lưu password/token trong Outbox; replica có cùng business schema nên được đồng bộ cả scalar columns theo model. Không log payload hoặc giá trị nhạy cảm.
5. Target transaction bao toàn batch: UPDATE theo PK nếu tồn tại; INSERT nếu thiếu; DELETE khi tombstone. SQLServer INSERT giữ identity bằng IDENTITY_INSERT ON/OFF trên cùng target connection; SQLite dùng explicit PK. ReadConnection dùng chung query/projector cần SELECT/INSERT/UPDATE/DELETE và quyền ALTER trên bảng identity để SET IDENTITY_INSERT; không thể dùng tài khoản chỉ đọc. Host kiểm tra MARS=false trên WriteConnection để caller savepoint đáng tin cậy (D05). Giữ audit đã đọc từ source, không phát Outbox/audit mới. Những property computed/rowversion không ghi (không có ở model hiện tại). Provider ngoài SQLServer/SQLite fail closed. Trường hợp target update/insert/delete/commit lỗi rollback, không ack; poison message giữ pending và dừng batch để vận hành sửa nguyên nhân.
6. Sau target commit mới đặt ProcessedAtUtc=UTC và SaveChanges source (chỉ Outbox). Ack lỗi/cancel sau target commit để pending cho vòng sau: replay current state idempotent. Lỗi trước target commit không đổi replica; khóa dispose trong mọi nhánh. Source có cập nhật khi projection đang chạy vẫn sinh message pending tiếp theo, hội tụ ở lần sau.

## Kiểm thử và kiểm tra L

| Subject | Nhóm áp dụng | Kịch bản và invariant |
| --- | --- | --- |
| Processor | Happy/Boundary/Error/SideEffect/Transaction/State/Output | Lock held tới ack; empty/no-lock không apply; target lỗi/cancel không ack; ack lỗi replay; không high-water |
| Relational store | Validation/Happy/Error/Transaction/State | Identity explicit, update giữ audit, composite key, delete missing source, invalid entity/key không ghi, whole batch rollback, duplicate replay/current state, pending lower ID |
| Worker | Error/Cancellation/Boundary | Disabled không resolve scope; dependency lỗi được log/retry; cancel không crash host |
| SQLServer dialect | Validation/SideEffect | Safe quoted metadata identifiers/parameter values; IDENTITY_INSERT cleanup; negative application-lock codes fail closed (review/mocks) |

Authorization/tenant không áp dụng: không có endpoint mới, không input người dùng; service nội bộ có DB credentials riêng. Business rules không đổi. SQLite không chứng minh sp_getapplock hoặc SQL Server IDENTITY_INSERT; không chạy DB ngoài theo ràng buộc.

ImpactRow: write SaveChanges capture T01 -> pending -> processor/store T02 -> query read host. ContractChange: thêm nội bộ service/Outbox, không thay public API; legacy disabled không resolve store. CrossModuleScenario: tạo row+Outbox; rollback caller không pending; replica lỗi/restart; commit lệch ID; composite/hard-delete; malformed message không ack. DataChangePlan applicable=false trong phiên, DBA tạo schema/read baseline sau này; rollback deployment trỏ read về Write DB và tắt worker, giữ Outbox. Replica chỉ service này được ghi; target schema/FK ngoài model cần đánh giá riêng trước triển khai.

## Review đặc tả

Spec Sentinel tự kiểm: không placeholder/quyết định nghiệp vụ thiếu; giới hạn SQLServer test được khai báo. Current model không FK nên batch raw SQL phù hợp; schema tương lai thêm FK cần chiến lược dependency mới. Không có thay đổi public API, auth hay business validation. Chờ host Spec approval; chưa viết plan/code.
