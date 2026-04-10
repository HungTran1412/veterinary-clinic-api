namespace VeterinaryClinic.Business
{
    /// <summary>
    /// Dịch vụ để xử lý mã hóa và xác thực mật khẩu.
    /// </summary>
    public interface IBcryptPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }
}
