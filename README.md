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

- `ConnectionStrings:DefaultConnection`: chuỗi kết nối SQL Server.
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
- Publish project `VeterinaryClinic.API`.

## Ghi Chú Phát Triển

- API đang dùng 2 DbContext: `VeterinaryClinicDataContext` cho ghi và `VeterinaryClinicReadDataContext` cho đọc.
- Audit fields như created/modified user và date được xử lý trong `SaveChangesAsync` của write DbContext.
- Localization đọc file JSON trong `VeterinaryClinic.API/wwwroot/Localization`.
- Static files được phục vụ từ `VeterinaryClinic.API/wwwroot`.
- QuestPDF đang dùng license community.
- Hangfire dashboard hiện đang map tại `/hangfire`; cần bổ sung authorization filter nếu đưa lên môi trường production.
