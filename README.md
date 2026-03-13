# 🐾 Veterinary Clinic Management System (API)

Hệ thống API quản lý phòng khám thú y được xây dựng trên nền tảng **.NET 8**, tuân thủ kiến trúc phân lớp (Clean Architecture) và áp dụng các mẫu thiết kế hiện đại nhằm đảm bảo tính mở rộng, hiệu năng và bảo mật.

## 🏗 Kiến trúc hệ thống (Architecture)

Dự án được tổ chức theo mô hình **CQRS (Command Query Responsibility Segregation)** thông qua thư viện **MediatR**, chia làm 5 project chính:

1.  **VeterinaryClinic.API**: Tầng giao diện API, chứa Controllers, Middleware và cấu hình hệ thống.
2.  **VeterinaryClinic.Business**: Chứa logic nghiệp vụ, các Handler xử lý Command/Query và các DTOs (Models).
3.  **VeterinaryClinic.Data**: Tầng truy cập dữ liệu, sử dụng Entity Framework Core với cơ chế tách biệt Read/Write DataContext để tối ưu hiệu suất.
4.  **VeterinaryClinic.Infrastructure**: Triển khai các dịch vụ kỹ thuật hạ tầng như Gửi Mail, Mã hóa mật khẩu, JWT Service.
5.  **VeterinaryClinic.Shared**: Chứa các tiện ích dùng chung (Utils), Helper, Base classes và Templates.

## 🚀 Công nghệ sử dụng (Tech Stack)

- **Backend**: .NET 8 (C#)
- **Database**: SQL Server (Entity Framework Core)
- **Caching**: Redis (StackExchange.Redis)
- **Xác thực**: JWT (JSON Web Token) với cơ chế Access Token & Refresh Token
- **Bảo mật**: BCrypt.Net để băm mật khẩu
- **Giao tiếp**: MediatR (In-process Messaging)
- **Mapping**: AutoMapper (với cấu hình tối ưu cho Collection)
- **Logging**: Serilog (Console & File)
- **Localization**: Hệ thống đa ngôn ngữ tùy chỉnh (Custom Json Localizer)
- **API Documentation**: Swagger UI (đã tùy chỉnh giao diện và thêm nút Copy Endpoint)
- **Email**: MailKit & MimeKit

## 🛠 Các tính năng đã hoàn thành

### 🔐 Bảo mật & Xác thực
- [x] Đăng nhập linh hoạt bằng **Username / Email / Số điện thoại**.
- [x] Cơ chế **JWT Authentication** bảo mật cao.
- [x] Hệ thống **Refresh Token** giúp duy trì phiên đăng nhập mà không làm gián đoạn người dùng.
- [x] Mã hóa mật khẩu một chiều bằng **BCrypt**.
- [x] Tự động ghi vết (Audit Logging): `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` được xử lý tự động trong DbContext.

### 🏥 Quản lý Chuyên ngành (Specializations)
- [x] CRUD đầy đủ (Thêm, Sửa, Xóa mềm, Lấy chi tiết).
- [x] Tìm kiếm nâng cao với phân trang và sắp xếp động.
- [x] Tích hợp **Redis Cache** cho danh sách Combobox để tăng tốc độ phản hồi.

### 💉 Quản lý Dịch vụ (Services)
- [x] CRUD dịch vụ thú y.
- [x] Lọc dịch vụ theo chuyên ngành, trạng thái hoạt động và khả dụng.
- [x] Quản lý giá và thời gian thực hiện dịch vụ.

### 👥 Quản lý Người dùng (Users)
- [x] Admin tạo tài khoản cho nhân viên/bác sĩ.
- [x] Tự động **gửi email thông báo** thông tin tài khoản khi được cấp mới.
- [x] Chức năng **Đổi mật khẩu** an toàn (kiểm tra mật khẩu cũ, độ phức tạp mật khẩu mới).
- [x] Lọc và quản lý danh sách nhân sự.
- [x] **Email Logging**: Lưu lại toàn bộ lịch sử gửi mail vào database để theo dõi.

### 🌍 Tính năng hệ thống
- [x] **Multi-language**: Hỗ trợ Tiếng Việt (vi-VN) và Tiếng Anh (en-US) thông qua file JSON.
- [x] **Custom Swagger**: Giao diện Swagger chuyên nghiệp, hỗ trợ chèn nút Copy URL trực tiếp vào giao diện bằng JavaScript.
- [x] **Validation**: Hệ thống kiểm tra định dạng Email, độ phức tạp mật khẩu bằng Regex.

## 📋 Hướng dẫn cài đặt

1.  **Clone dự án**:
    ```sh
    git clone https://github.com/your-repo/veterinary-clinic-api.git
    ```
2.  **Cấu hình**:
    - Đổi tên `appsettings.json.example` thành `appsettings.json`.
    - Cập nhật `ConnectionStrings` (SQL Server) và `EmailSettings` (SMTP Server/App Password).
    - Cấu hình `JwtSettings:Secret` với một chuỗi bảo mật dài.
3.  **Cập nhật Database**:
    ```sh
    dotnet ef database update --project VeterinaryClinic.Data --startup-project VeterinaryClinic.API
    ```
4.  **Chạy ứng dụng**: Nhấn `F5` trong Visual Studio/Rider hoặc:
    ```sh
    dotnet run --project VeterinaryClinic.API
    ```

## 📝 Ghi chú
- API mặc định chạy tại: `https://localhost:44360/swagger`
- Các tài nguyên tĩnh (Localization, JS tùy chỉnh) nằm trong thư mục `wwwroot`.
