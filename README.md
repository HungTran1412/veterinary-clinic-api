# Veterinary Clinic API

Backend API cho hệ thống quản lý phòng khám thú y, được xây dựng bằng .NET 8 theo hướng phân lớp, CQRS với MediatR, Entity Framework Core và SQL Server. Dự án tập trung vào các nghiệp vụ đặt lịch khám, quản lý thú cưng, bác sĩ, chuyên khoa, dịch vụ, lịch làm việc, hồ sơ bệnh án, hóa đơn, thanh toán và thông báo thời gian thực.

 clone https://github.com/HungTran1412/veterinary-clinic-ui để lấy project ui của hệ thống hoặc bạn có thể tự phát triển giao diện riêng của mình

## Công Nghệ Đang Sử Dụng

### Nền Tảng Chính

- .NET 8, C# 12
- ASP.NET Core Web API
- Entity Framework Core 8
- SQL Server
- Redis
- Swagger / OpenAPI với Swashbuckle
- GitHub Actions để build/publish trên Windows runner

### Thư Viện Và Dịch Vụ

- MediatR 12: triển khai CQRS cho Command/Query/Handler
- AutoMapper 13 và AutoMapper.Collection: mapping DTO/model
- Serilog: logging cho ứng dụng
- JWT Bearer Authentication: access token và refresh token
- BCrypt.Net-Next: băm và xác thực mật khẩu
- StackExchange.Redis và Microsoft Distributed Cache: cache và thao tác Redis
- SignalR + Redis backplane: thông báo thời gian thực
- Hangfire + SQL Server storage: xử lý background job và dashboard
- MailKit / MimeKit: gửi email SMTP
- CloudinaryDotNet: upload và quản lý ảnh
- QuestPDF: tạo file PDF hóa đơn
- WorkflowEngine.NETCore: quản lý workflow/trạng thái nghiệp vụ
- VNPAY: tạo URL thanh toán và xử lý callback thanh toán
- Custom JSON Localization: hỗ trợ `vi-VN` và `en-US`

## Kiến Trúc Solution

Solution gồm 5 project chính:

| Project | Vai trò |
| --- | --- |
| `VeterinaryClinic.API` | Tầng Web API: controllers, Swagger, CORS, authentication, localization, SignalR hub, Hangfire dashboard và DI composition root. |
| `VeterinaryClinic.Business` | Tầng nghiệp vụ: Command/Query/Handler theo MediatR, models, validators/logic nghiệp vụ, cache service interface và workflow/state machine. |
| `VeterinaryClinic.Data` | Tầng dữ liệu: EF Core DbContext, entities, migrations, read/write data context. |
| `VeterinaryClinic.Infrastructure` | Hiện thực các dịch vụ hạ tầng: email, JWT, password hashing, Cloudinary, Redis handler, QuestPDF. |
| `VeterinaryClinic.Shared` | Thành phần dùng chung: constants, enums, base entity/model, response wrapper, helpers, localization resources, config models và context accessor. |

## Các Module Nghiệp Vụ Chính

- Xác thực và phân quyền: đăng ký, đăng nhập, refresh token, đăng xuất, quên mật khẩu, verify email.
- Quản lý người dùng: admin, nhân viên, bác sĩ, thông tin người dùng đang đăng nhập, đổi mật khẩu.
- Quản lý thú cưng và ảnh đại diện thú cưng.
- Quản lý chuyên khoa, chuyên khoa của bác sĩ và dịch vụ khám.
- Quản lý lịch hẹn, quy trình xử lý lịch hẹn và trạng thái workflow.
- Quản lý ca làm việc, lịch làm việc và đăng ký lịch làm việc.
- Quản lý hồ sơ bệnh án, hóa đơn, bill PDF và invoice.
- Thanh toán VNPAY và xử lý return callback.
- Dashboard thống kê và doanh thu.
- Thông báo realtime qua SignalR và lưu notification.
- Email log để theo dõi lịch sử gửi email.
- Cache/Redis endpoint phục vụ kiểm tra và quản trị cache.

## Cấu Trúc API

Phần lớn endpoint dùng prefix:

```text
/veterinary-clinic/v1
```

Một số nhóm endpoint:

- `/authorization`
- `/forgot-password`
- `/user-manager`
- `/pets`
- `/photo-upload`
- `/specializations`
- `/doctor-specializations`
- `/services`
- `/appointments`
- `/medical-records`
- `/shift-templates`
- `/work-schedule`
- `/work-schedule-registrations`
- `/payments`
- `/bills`
- `/invoices`
- `/notifications`
- `/dashboard`
- `/email-logs`
- `/cache`
- `/redis`

SignalR hub mặc định:

```text
/veterinary-clinic/v1/notifications/hub
```

Hangfire dashboard:

```text
/hangfire
```

## Yêu Cầu Môi Trường

- .NET SDK 8.x. Repo có `global.json` ghim SDK `8.0.0` và cho phép roll-forward lên latest minor.
- SQL Server.
- Redis server.
- Tài khoản SMTP nếu cần gửi email.
- Tài khoản Cloudinary nếu cần upload ảnh.
- Tài khoản VNPAY sandbox/production nếu cần thanh toán.
- Visual Studio 2022, Rider hoặc VS Code.

## Cấu Hình Ứng Dụng

Sao chép file cấu hình mẫu:

```powershell
Copy-Item VeterinaryClinic.API\appsettings.json.example VeterinaryClinic.API\appsettings.json
```

Cập nhật các nhóm cấu hình quan trọng trong `VeterinaryClinic.API/appsettings.json`:

- `ConnectionStrings:WriteConnection`: SQL Server nghiệp vụ ghi, kiểm tra điều kiện command và Hangfire storage.
- `ConnectionStrings:ReadConnection`: SQL Server đọc cho query và đích ghi của service đồng bộ.
- `ReadModelSynchronization`: bật Outbox và service nền, chu kỳ đọc pending và kích thước batch.
- `ConnectionStrings:DefaultConnection`: tương thích cấu hình cũ một DB khi chưa bật đồng bộ.
- `ConnectionStrings:Redis`: chuỗi kết nối Redis.
- `CorsSettings:AllowedOrigins`: domain frontend được phép gọi API trong production.
- `AdminSettings`: tài khoản admin mặc định sẽ được seed khi khởi động.
- `JwtSettings`: secret, issuer, audience và thời gian sống token.
- `EmailSettings`: SMTP, sender và URL frontend/backend cho email.
- `CloudinarySettings`: cloud name, API key và API secret.
- `VnPaySettings`: mã terminal, hash secret, payment URL và return URL.
- `SignalR:Hubs:NotificationUrl`: đường dẫn hub thông báo.
- `ClinicInfo`: thông tin phòng khám dùng trong tài liệu/PDF.

Không commit file `appsettings.json` có secret thật lên repository.

## Tách Read DB Và Write DB

File mẫu dùng `VeterinaryCliniceDB` và `VeterinaryClinicReadDB`, bật `ReadModelSynchronization:Enabled=true`. Chỉ áp dụng sau khi các điều kiện triển khai dưới đây đã được chuẩn bị. Code không tự tạo bảng Outbox, tạo replica hay sao chép dữ liệu cũ lúc startup. Khi còn dùng một DB, giữ `DefaultConnection` và bỏ nhóm đồng bộ hoặc đặt `Enabled=false`.

Luồng xử lý:

1. `VeterinaryClinicDataContext.SaveChanges` lưu thay đổi nghiệp vụ và khóa bản ghi trong `vcReadModelOutbox` cùng transaction trên Write DB. Outbox không lưu snapshot mật khẩu/token.
2. Sau khi transaction có ghi Outbox commit thành công, write context gửi tín hiệu đánh thức `ReadModelSynchronizationWorker` trong cùng process. Transaction do caller mở chỉ gửi tín hiệu khi caller commit thực tế, không gửi ngay sau `SaveChanges` bên trong transaction. Worker lấy pending theo `Id`, tối đa `BatchSize` (mặc định 100), xử lý batch tiếp theo ngay khi lượt trước có dữ liệu; khi rỗng thì chờ tín hiệu hoặc hết `PollIntervalSeconds` (mặc định 5 giây). SQL Server application lock trên Write DB đảm bảo chỉ một consumer xử lý, kể cả có nhiều API instance.
3. Consumer đọc lại dữ liệu hiện tại tại Write DB, upsert hoặc xóa ở Read DB trong transaction, giữ nguyên primary key, khóa ghép và thông tin audit. Sau khi Read DB commit mới đánh dấu Outbox hoàn tất.
4. Nếu lỗi, Outbox còn pending và worker nghỉ theo `PollIntervalSeconds` trước khi thử lại; tín hiệu mới không bỏ qua khoảng nghỉ này. Nếu replica đã commit nhưng ack lỗi, lần sau áp dụng lại trạng thái hiện tại; không dùng checkpoint lớn nhất nên không bỏ mất message có `Id` thấp nhưng commit muộn. Việc ack Outbox không phát thêm tín hiệu.

Command và `check-token` dùng Write DB authoritative. Query thông thường dùng Read DB và có thể trễ hơn lệnh ghi; không có bảo đảm đọc thấy ngay dữ liệu vừa ghi. Service lỗi không làm thất bại một command đã commit. Khi bật đồng bộ, `RedisCacheService.GetOrCreate/Set` bỏ qua cache để không giữ kết quả replica cũ sau khi đồng bộ đã hoàn tất; cấu hình một DB vẫn dùng cache như trước. Redis/SignalR vẫn được cấu hình và sử dụng cho các mục đích khác.

### Điều kiện cấu hình và quyền

- Khi bật đồng bộ, khai báo đủ `WriteConnection` và `ReadConnection`, trỏ tới hai database vật lý khác nhau. Validator kiểm tra server/catalog, không suy ra được mọi DNS alias/listener; người triển khai phải xác minh đích thực tế. Hai chuỗi chỉ khác username hoặc tùy chọn kết nối vẫn là cùng DB và bị từ chối.
- `WriteConnection` phải bỏ `MultipleActiveResultSets=true` hoặc đặt `false`: Outbox dùng savepoint trong transaction do command sở hữu. Startup từ chối MARS khi đồng bộ bật. Ambient `TransactionScope` và transaction không hỗ trợ savepoint bị từ chối trước khi ghi.
- `ReadConnection` được dùng chung cho query và projector, cần SELECT/INSERT/UPDATE/DELETE trên bảng business và quyền cho `SET IDENTITY_INSERT` (owner hoặc ALTER trên bảng identity). `NoTracking` cho query không thay thế phân quyền SQL Server. Write credential cần CRUD business/Outbox và quyền chạy application lock.
- `PollIntervalSeconds` từ 1 đến 3600; `BatchSize` từ 1 đến 1000. Đây là chu kỳ thử lại, không phải cam kết độ trễ tối đa; backlog, lỗi DB hoặc message lỗi có thể kéo dài thời gian hội tụ.
- Read DB phải có schema business giống Write DB, giữ nguyên identity/primary key/index và cả các stored procedure `sp_GetMedicalRecordById`, `sp_DashboardRevenueOverview`. `sp_GetCandidateDoctors` phục vụ command vẫn chạy tại Write DB. Không đồng bộ bảng Hangfire, Outbox hay lịch sử migration sang replica.

### Hợp đồng bảng Outbox để chuẩn bị khi triển khai

Tầng Data map `ReadModelOutboxMessage` vào `vcReadModelOutbox` trên Write DB, schema mặc định của context. DBA cần chuẩn bị theo hợp đồng sau; thay đổi source này không kèm migration hay thực thi DDL:

| Cột | Kiểu SQL Server | Ràng buộc |
| --- | --- | --- |
| `Id` | `bigint` | Primary key, identity |
| `EntityType` | `nvarchar(512)` | NOT NULL, tên entity từ EF metadata |
| `KeyValuesJson` | `nvarchar(max)` | NOT NULL, JSON object tên thuộc tính khóa -> giá trị |
| `CreatedAtUtc` | `datetime2` | NOT NULL, do ứng dụng ghi UTC |
| `ProcessedAtUtc` | `datetime2` | NULL khi pending |

Index pending: `(ProcessedAtUtc, Id)`. Ví dụ khóa đơn `{"Id":12}`, khóa ghép `{"DoctorId":7,"SpecializationId":3}`. Không thêm business column hay operation vào payload; consumer đọc lại trạng thái mới nhất. Bảng cần tồn tại trước khi bật capture; nếu ghi Outbox lỗi thì lần SaveChanges tương ứng rollback cả business.

### Bootstrap dữ liệu hiện có và cutover

1. Dừng traffic ghi và các API/job có thể ghi dữ liệu. Tạo/chuẩn bị hai DB, schema business và bảng Outbox bằng quy trình DBA được phê duyệt riêng.
2. Copy baseline toàn bộ bảng business từ Write DB sang Read DB trong thời gian dừng ghi, giữ ID và khóa ghép. Copy cả stored procedure phục vụ query. Không copy Outbox hay Hangfire. Kiểm tra row count/khóa/dữ liệu theo từng bảng; việc tạo Read DB trống rồi chỉ bật worker không đồng bộ được các bản ghi cũ chưa từng phát Outbox.
3. Triển khai source và cấu hình hai kết nối, tắt MARS ở WriteConnection, bật đồng bộ. Khởi động API trong khi public traffic còn đóng. Seeder/Hangfire vốn có của hệ thống có thể ghi khi khởi động nên cũng phải được kiểm soát trong cửa sổ này.
4. Theo dõi Outbox pending, kiểm tra replica và các query/stored procedure trước khi mở traffic. Không bật đồng thời cơ chế khác ghi vào replica; mô hình hiện tại không khai báo FK thực trong EF, nếu schema vận hành có FK khác model cần đánh giá thứ tự đồng bộ trước cutover.

### Vận hành và giới hạn

Theo dõi số message `ProcessedAtUtc IS NULL`, tuổi message pending cũ nhất theo `CreatedAtUtc` và log worker. Message lỗi/payload không hợp lệ chặn cả batch và giữ pending để điều tra; không tự bỏ qua hay đánh dấu thành công. Sửa nguyên nhân/schema/data qua quy trình vận hành trước khi để service thử lại. Dọn các message đã processed theo retention do đội vận hành quyết định; không xóa pending. Code không tự dọn dữ liệu.

Tín hiệu đánh thức dùng Channel trong bộ nhớ, chỉ gộp yêu cầu kiểm tra chứ không chứa dữ liệu business. Outbox vẫn là nguồn bền vững. Polling tiếp tục hoạt động khi không có tín hiệu, process khởi động lại, consumer khác giữ khóa hoặc thay đổi được commit từ instance khác. Lỗi phát tín hiệu không làm thất bại lệnh ghi đã commit. Tối ưu này giảm thời gian chờ chủ động, không bảo đảm query đọc thấy ngay sau command; độ trễ thực tế còn phụ thuộc database, backlog và retry. Caller commit trực tiếp underlying ADO transaction thay vì API transaction EF không đi qua observer; polling vẫn dự phòng.

Capture chỉ bao phủ EF `SaveChanges` trên write context. Raw SQL, `ExecuteUpdate/Delete`, stored procedure ghi hay ứng dụng khác ghi thẳng DB không tự phát Outbox; cần tích hợp capture tương ứng trước khi sử dụng những đường ghi này. Keys-only đồng bộ trạng thái cuối cùng, không dùng làm audit lịch sử sự kiện. Thay đổi schema/entity cần được triển khai đồng bộ hai phía. Đây là đồng bộ một chiều, Read DB không là nguồn dữ liệu để ghi ngược.

Rollback vận hành: dừng traffic và worker, cấu hình ReadConnection cùng đích với WriteConnection (hoặc dùng lại DefaultConnection), tắt `Enabled`, rồi kiểm tra trước khi mở traffic. Giữ Write DB làm nguồn chính và giữ Outbox để điều tra/khôi phục; không chuyển ghi sang replica đang trễ.

Unit test dùng SQLite in-memory và mock/offline SQL command; không kết nối DB hệ thống. Chưa xác nhận runtime `sp_getapplock`/`IDENTITY_INSERT` trên SQL Server thật. Kiểm thử tích hợp SQL Server cần một môi trường riêng được cấp quyền trước triển khai.

```powershell
dotnet test VeterinaryClinic.Tests\VeterinaryClinic.Tests.csproj -m:1
```

## Khởi Tạo Database

Nếu chưa có `dotnet-ef`, cài đặt tool:

```powershell
dotnet tool install --global dotnet-ef
```

Cập nhật database từ migrations hiện có:

```powershell
dotnet ef database update --project VeterinaryClinic.Data --startup-project VeterinaryClinic.API --context VeterinaryClinicDataContext
```

Tạo migration mới khi thay đổi model:

```powershell
dotnet ef migrations add TenMigration --project VeterinaryClinic.Data --startup-project VeterinaryClinic.API --context VeterinaryClinicDataContext
```

## Chạy Dự Án

Restore và build solution:

```powershell
dotnet restore VeterinaryClinic.Solution.sln
dotnet build VeterinaryClinic.Solution.sln
```

Chạy API:

```powershell
dotnet run --project VeterinaryClinic.API
```

Theo `launchSettings.json`, profile project sẽ chạy trên:

```text
https://localhost:7001
http://localhost:5001
```

Swagger UI:

```text
https://localhost:7001/swagger
```

Khi chạy bằng IIS Express, SSL port đang cấu hình là:

```text
https://localhost:44360/swagger
```

## Build Và Publish

Build release:

```powershell
dotnet build VeterinaryClinic.Solution.sln --configuration Release
```

Publish API:

```powershell
dotnet publish VeterinaryClinic.API\VeterinaryClinic.API.csproj --configuration Release --output .\publish
```

Workflow CI hiện có tại `.github/workflows/dotnet-desktop.yml` sẽ:

- Restore solution.
- Build solution ở cấu hình Release.
- Chạy `VeterinaryClinic.Tests` ở cấu hình Release, dùng kết quả build trước đó. Test thất bại sẽ dừng CI trước bước publish.
- Publish project `VeterinaryClinic.API`.

## Ghi Chú Phát Triển

- API đang dùng 2 DbContext: `VeterinaryClinicDataContext` cho ghi và `VeterinaryClinicReadDataContext` cho đọc.
- Audit fields như created/modified user và date được xử lý trong `SaveChangesAsync` của write DbContext.
- Localization đọc file JSON trong `VeterinaryClinic.API/wwwroot/Localization`.
- Static files được phục vụ từ `VeterinaryClinic.API/wwwroot`.
- QuestPDF đang dùng license community.
- Hangfire dashboard hiện đang map tại `/hangfire`; cần bổ sung authorization filter nếu đưa lên môi trường production.
