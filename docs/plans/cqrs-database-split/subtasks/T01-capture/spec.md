# Đặc tả T01: ghi Transactional Outbox

runId: cqrs-db-split-20261020
taskId: T01-capture
workerId: /root/outbox_capture
specRevision: 3
overviewRevision: 2
sourceRefs: `VeterinaryClinic.Data/Systems/DataContext/VeterinaryClinicDataContext.cs` (constructor, OnModelCreating, SaveChangesAsync); `VeterinaryClinic.Data/Systems/Context/VcDoctorSpecializations.cs` (khóa ghép); `VeterinaryClinic.Business/Business/Bills/BillCommands/CreateBillCommand.cs` (transaction, nhiều SaveChangesAsync); `VeterinaryClinic.Business/Business/Appointments/AppointmentCommands/CreateAppointmentCommand.cs` (generated ID, transaction); `VeterinaryClinic.Data/VeterinaryClinic.Data.csproj` (EF Core SQL Server 8.0.2).

## Yêu cầu và ranh giới

AC02: khi bật capture, mỗi entity business Added/Modified/Deleted được lưu cùng bản ghi Outbox trong một transaction. Hỗ trợ generated identity, khóa ghép, xóa cứng, bốn overload SaveChanges sync/async, SaveChanges(false), transaction do caller sở hữu và nhiều lần save. Lỗi hoặc cancellation không được để lại business commit thiếu Outbox. AC07: unit test SQLite in-memory biệt lập, mỗi test có comment tiếng Việt UTF-8 đầu hàm. Không thay migration/snapshot, DB thật, cấu hình thật, API contract hoặc file ngoài quyền ghi.

Chưa có Wiki hay test project. Host cung cấp xUnit/SQLite 8.0.2; dùng Xunit.Assert theo quyết định host vì FluentAssertions không có sẵn.

## Hiện trạng và lựa chọn

Write context chỉ override SaveChangesAsync(CancellationToken), audit TrackedChangeEntity trước khi lưu. Các overload còn lại có thể bỏ qua audit. Các command hiện dùng transaction nhiều lần save; không cấu hình retry execution strategy. Read context dùng cùng entity nhưng không có Outbox.

So sánh: dual-write không atomic khi Read DB lỗi; CDC cần hạ tầng ngoài phạm vi; mở rộng write DbContext bằng Outbox đáp ứng quyết định người dùng. Giữ write context làm điểm capture chung, không sửa từng command.

## Hợp đồng công khai r1

Namespace `VeterinaryClinic.Data.Synchronization`:

- `ReadModelSynchronizationOptions`: `const string SectionName = "ReadModelSynchronization"`; `bool Enabled` mặc định false; `int PollIntervalSeconds = 5`; `int BatchSize = 100`.
- `ReadModelOutboxMessage`: `long Id`, `string EntityType`, `string KeyValuesJson`, `DateTime CreatedAtUtc`, `DateTime? ProcessedAtUtc`.
- Write context có `DbSet<ReadModelOutboxMessage> ReadModelOutboxMessages`. Constructor giữ hai tham số hiện tại, thêm optional `IOptions<ReadModelSynchronizationOptions>? synchronizationOptions = null`.
- Table `vcReadModelOutbox` không schema explicit; columns PascalCase đúng tên thuộc tính; Id generated-on-add/SQL Server bigint identity; EntityType required max 512; KeyValuesJson required; pending index `(ProcessedAtUtc, Id)`. Không hard-code HasColumnType: SQL Server tự map string không giới hạn thành nvarchar(max), DateTime thành datetime2; SQLite dùng kiểu tương thích provider.
- EntityType là tên EF `entry.Metadata.Name`; KeyValuesJson là JSON object tên primary-key property theo EF → giá trị CLR thật. Ví dụ `{ "Id": 12 }`, `{ "DoctorId": 7, "SpecializationId": 3 }`. Không Operation, không snapshot, không password/token hay dữ liệu ngoài PK. Consumer phải dùng metadata key theo thứ tự, không dựa vào thứ tự JSON.
- Outbox không phải business entity và không tự capture; read context không map bảng Outbox. Host/T02 consume hợp đồng này; host chịu trách nhiệm DI và cấu hình.

## Luồng ghi

Áp dụng audit hiện có cho cả bốn overload; khi capture disabled, chỉ gọi base và giữ hành vi SaveChanges/acceptAllChangesOnSuccess. Khi enabled, DetectChanges và lấy Added/Modified/Deleted, loại Outbox, chỉ entity mapped có PK. Không thay đổi business thì base.SaveChanges bình thường (cho phép ack Outbox không sinh message).

Chụp state/current/original/temporary/modified của entry business để khôi phục khi atomic save thất bại. Nếu không có transaction: mở local transaction; nếu caller có transaction: tạo savepoint bao quanh toàn bộ lần save và không commit transaction caller. Nếu caller transaction không hỗ trợ savepoint, reject trước khi ghi. SQL Server split mode bắt buộc MARS=false hoặc bỏ tùy chọn MARS; host cấu hình startup validator từ chối MARS=true khi sync enabled, bảo vệ Bill/Registration trước khi nhận request. Ambient TransactionScope khi capture enabled bị reject trước khi ghi vì không có local transaction/savepoint đủ khả năng phục hồi; repo không dùng ambient transaction.

Gọi base.SaveChanges(false) đúng một lần để lưu business và lấy primary key do DB sinh; serialize key từ entry sau save. Dùng DbCommand parameterized INSERT vào vcReadModelOutbox trên chính connection/transaction của write context. SQL chỉ dùng identifier cố định và parameters; hỗ trợ SQL Server cùng SQLite test. Không chạy lại business SaveChanges để ghi Outbox.

Chỉ khi ghi hết Outbox thành công mới commit local transaction hoặc release savepoint. AcceptAllChanges chỉ khi caller yêu cầu true và sau khi atomic save đã thành công. Trả số business entries mà base.SaveChanges trả về, không tính Outbox được INSERT trực tiếp. Với caller transaction, các lần save có Outbox tương ứng trong cùng transaction caller; caller commit/rollback tất cả. Sau rollback do caller chủ động, EF tracker có hành vi chuẩn hiện có: caller cần discard/reload context.

Nếu business/Outbox/cancellation lỗi: rollback local transaction hoặc savepoint bằng token không bị hủy, khôi phục tracker từ snapshot, rethrow lỗi ban đầu; caller có thể sửa lỗi rồi retry cùng context mà không có message cũ bị theo dõi hoặc nhân đôi. Không dùng high-water ID/checkpoint trong capture. Khi commit response không rõ do mất kết nối, chịu giới hạn transaction commit ambiguity chuẩn EF; không hứa exactly-once, consumer idempotent.

## Kiểm thử và C6

| Method | Nhóm áp dụng | Kịch bản chính | Hành vi bảo vệ |
| --- | --- | --- | --- |
| SaveChanges sync/async | Happy path, Side effect, Output, State | Thêm identity, sửa, xóa, khóa ghép, audit, disabled | Đúng PK, keys-only payload, output không cộng Outbox |
| SaveChanges(bool)/Async(bool) | Boundary, State | false giữ Added/Modified/Deleted tới AcceptAllChanges | Không vô tình accept hoặc lưu business hai lần |
| Atomic save | Error, Transaction, Cancellation | Thiếu bảng Outbox, lỗi business, pre/mid-cancel; retry context | Không commit một nửa, không lưu stale Outbox |
| Caller transaction | Transaction, Edge | Nhiều save rồi commit/rollback; save lỗi sau lần thành công | Không commit caller; savepoint giữ phần trước, hủy phần lỗi |
| Ambient/unsupported savepoint | Validation, Error | TransactionScope và transaction không có savepoint | Fail fast trước business write |

Authorization/permission không áp dụng: không endpoint hay quyền mới; giữ logic command hiện có. Kiểm thử chỉ tạo schema/data SQLite in-memory. SQL Server DB integration NotRun theo ràng buộc người dùng; review SQL parameterization và transaction provider dùng chung.

ImpactRow: tất cả write consumers (command, EmailService, SignalR, seeder) do T01 sở hữu thay đổi persistence; T02 đọc Outbox và host DI là consumers hợp đồng mới. ContractChange: constructor hai tham số còn hoạt động, cấu hình thiếu options mặc định disabled; SaveChanges output giữ business count; sync và bool overload nay audit đầy đủ. CrossModuleScenario: identity insert → Outbox PK thật → T02 reread latest; caller rollback → không business/Outbox; lỗi INSERT Outbox → rollback/retry không trùng.

DataChangePlan: applicable=false cho phiên thực thi; không migration/DB ngoài. Khi triển khai DBA chuẩn bị bảng theo mapping; dữ liệu cũ do host tài liệu baseline copy. Không tự EnsureCreated/Migrate/startup bootstrap. Không có câu hỏi nghiệp vụ còn mở; cần host duyệt giới hạn ambient/savepoint và hợp đồng r1.

## Spec Sentinel tự kiểm

Phase 0 dùng lại điều tra source. Không placeholder/không thay endpoint/tenant filtering hiện có. Rủi ro chính: metadata generated key và tracker recovery, được test relational thật SQLite. Mọi consumer SaveChanges đã tìm bằng rg; nhiều save transaction có trong Bill/Appointment. Review nội bộ khuyến nghị host approve sau xác nhận contract; đây không phải host approval. Chờ Spec gate trước plan.
