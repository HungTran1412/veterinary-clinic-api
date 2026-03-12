using BCrypt.Net;
using VeterinaryClinic.Business.Services; // Using Interface từ Business

namespace VeterinaryClinic.Infrastructure.Services
{
    public class PasswordHasher : IBcryptPasswordHasher
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            try 
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }
    }
}
