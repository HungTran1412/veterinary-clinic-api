using System.Text.RegularExpressions;

namespace VeterinaryClinic.Shared
{
    public static class ValidationUtils
    {
        // Regex cho Email chuẩn
        private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        // Regex cho Mật khẩu: Ít nhất 1 chữ hoa, 1 chữ thường, 1 số, 1 ký tự đặc biệt, độ dài tối thiểu 8 (tuỳ chọn)
        // (?=.*[a-z]): Có ít nhất 1 chữ thường
        // (?=.*[A-Z]): Có ít nhất 1 chữ hoa
        // (?=.*\d): Có ít nhất 1 số
        // (?=.*[@$!%*?&]): Có ít nhất 1 ký tự đặc biệt
        private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, EmailPattern);
        }

        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            return Regex.IsMatch(password, PasswordPattern);
        }

        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;
            // Kiểm tra xem username có chứa khoảng trắng hay không
            return !username.Contains(" ");
        }
    }
}
