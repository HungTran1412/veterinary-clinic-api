namespace VeterinaryClinic.Infrastructure.Services
{
    /// <summary>
    /// Dịch vụ để xử lý mã hóa và xác thực mật khẩu.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Mã hóa một mật khẩu dạng chuỗi thuần.
        /// </summary>
        /// <param name="password">Mật khẩu cần mã hóa.</param>
        /// <returns>Chuỗi mật khẩu đã được mã hóa.</returns>
        string HashPassword(string password);

        /// <summary>
        /// Xác thực một mật khẩu dạng chuỗi thuần với một chuỗi đã được mã hóa.
        /// </summary>
        /// <param name="password">Mật khẩu chuỗi thuần người dùng nhập vào.</param>
        /// <param name="hashedPassword">Mật khẩu đã mã hóa lấy từ database.</param>
        /// <returns>True nếu mật khẩu khớp, ngược lại là False.</returns>
        bool VerifyPassword(string password, string hashedPassword);
    }
}
