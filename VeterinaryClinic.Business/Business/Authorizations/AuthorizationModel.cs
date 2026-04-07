using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Business
{
    public abstract record AuthorizationBaseModel
    {
        [Required]
        public string LoginIdentifier { get; init; }

        [Required] 
        public string Password { get; init; }
    }

    public record LoginModel : AuthorizationBaseModel
    {
    }

    public record LoginResponseModel
    {
        public int Id { get; init; }
        public string FullName { get; init; }
        public string UserName { get; init; }
        public string Email { get; init; }
        public string Role { get; init; }
        public string AccessToken { get; init; }
        public string RefreshToken { get; init; }
    }

    public record RefreshTokenModel
    {
        [Required] public string AccessToken { get; init; }

        [Required] public string RefreshToken { get; init; }
    }
}