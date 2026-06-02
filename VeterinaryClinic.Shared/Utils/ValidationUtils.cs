using System;
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

    public static class GenerateCodeUtils
    {
        /// <summary>
        /// Sinh mã ngẫu nhiên với một tiền tố.
        /// </summary>
        public static string GenerateCode(string prefix)
        {
            int randomNumber = Random.Shared.Next(10000000, 100000000);
            return $"{prefix}{randomNumber}";
        }

        /// <summary>
        /// Sinh mã theo ngày với một tiền tố và một phần số ngẫu nhiên.
        /// Định dạng: Prefix-yyMMdd-xxxx
        /// </summary>
        /// <param name="prefix">Tiền tố cho mã (ví dụ: BILL, INV).</param>
        /// <returns>Một mã duy nhất có chứa thông tin ngày tháng.</returns>
        public static string GenerateCodeByDaily(string prefix)
        {
            string datePart = DateTime.UtcNow.ToString("yyMMdd");
            int randomNumber = Random.Shared.Next(1000, 10000); // 4-digit random number
            return $"{prefix}-{datePart}-{randomNumber}";
        }
    }
}
