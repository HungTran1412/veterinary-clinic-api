using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public abstract record UserBaseModel
    {
        public int Id { get; init; }
        
        [Required(ErrorMessage = "user.code.required")]
        public string Code { get; init; }
        
        [Required(ErrorMessage = "user.username.required")]
        [MaxLength(100)]
        public string UserName { get; init; }
        
        [Required(ErrorMessage = "user.email.required")]
        public string Email { get; init; }
        
        [Required(ErrorMessage = "user.full_name.required")]
        public string FullName { get; init; }
        
        [Required(ErrorMessage = "user.phone_number.required")]
        public string PhoneNumber { get; init; }
        
        public int? Gender { get; init; }
        
        public string? Address { get; init; }
        public string? AvatarUrl { get; init; }
        
        [Required(ErrorMessage = "user.role.required")]
        public string Role { get; init; }
        
        public bool IsActive { get; init; } = true;

        public int Order { get; init; }
        public DateTime? CreatedDate { get; init; }
    }

    public record UserModel : UserBaseModel
    {
        [Required(ErrorMessage = "user.password.required")]
        public string Password { get; init; }
    }
    
    public record CreateUserModel : UserModel
    {
        public int? CreatedUserId { get; init; }
    }

    public record UpdateUserModel : UserBaseModel
    {
        public int? ModifiedUserId { get; init; }

        public void UpdateEntity(VcUsers entity)
        {
            entity.Username = this.UserName;
            entity.FullName = this.FullName;
            entity.PhoneNumber = this.PhoneNumber;
            entity.AvatarUrl = string.IsNullOrEmpty(this.AvatarUrl) ? "" : this.AvatarUrl;
            entity.Gender = this.Gender;
            entity.Address = this.Address;
            entity.Order = this.Order;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public record UpdatePasswordUserModel
    {
        public int? ModifiedUserId { get; init; }
        public string OldPassword { get; init; }
        public string NewPassword { get; init; }
        public string ConfirmPassword { get; init; }

        public void UpdatePassword(VcUsers entity)
        {
            entity.Password = this.NewPassword;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public record UserRegisterModel
    {
        [Required]
        public string UserName { get; init; }
        [Required]
        public string FullName { get; init; }
        [Required]
        public string PhoneNumber { get; init; }
        [Required]
        public string Email { get; init; }
        [Required]
        public string Password { get; init; }
        [Required]
        public string RepeatPassword { get; init; }
    }

    public record UserFilterModel : BaseQueryFilterModel
    {
        public string? Role { get; init; }
    }
}
