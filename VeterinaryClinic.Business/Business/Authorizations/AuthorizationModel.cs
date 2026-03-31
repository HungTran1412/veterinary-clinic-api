using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Business
{
    public record AuthorizationBaseModel
    {
        [Required]
        public string LoginIdentifier { get; set; }

        [Required] 
        public string Password { get; set; }
    }

    public record LoginModel : AuthorizationBaseModel
    {
    }

    public record LoginResponseModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public record RefreshTokenModel
    {
        [Required] public string AccessToken { get; set; }

        [Required] public string RefreshToken { get; set; }
    }
}