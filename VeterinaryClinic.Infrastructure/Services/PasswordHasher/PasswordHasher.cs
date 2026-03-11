using BCrypt.Net;

namespace VeterinaryClinic.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            // Sử dụng BCrypt để mã hóa mật khẩu.
            // Hàm này tự động sinh ra Salt và hash mật khẩu.
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            // Sử dụng BCrypt để kiểm tra mật khẩu.
            // Hàm này sẽ lấy Salt từ hashedPassword và băm password nhập vào, sau đó so sánh 2 chuỗi hash.
            try 
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                // Trả về false nếu có lỗi (ví dụ format hash không đúng)
                return false;
            }
        }
    }
}
