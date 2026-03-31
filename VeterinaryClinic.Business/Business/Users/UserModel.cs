using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{ 
    public record UserBaseModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "user.code.required")]
        public string Code { get; set; }
        
        [Required(ErrorMessage = "user.username.required")]
        [MaxLength(100)]
        public string UserName { get; set; }
        
        [Required(ErrorMessage = "user.email.required")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "user.full_name.required")]
        public string FullName { get; set; }
        
        [Required(ErrorMessage = "user.phone_number.required")]
        public string PhoneNumber { get; set; }
        
        public int? Gender { get; set; }
        
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        
        [Required(ErrorMessage = "user.role.required")]
        public string Role { get; set; }
        
        public bool IsActive { get; set; } = true;

        public int Order { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public record UserModel : UserBaseModel
    {
        [Required(ErrorMessage = "user.password.required")]
        public string Password { get; set; }
    }
    
    public record CreateUserModel : UserModel
    {
        public int? CreatedUserId { get; set; }
    }

    public record UpdateUserModel : UserBaseModel
    {
        public int? ModifiedUserId { get; set; }

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
        public int? ModifiedUserId { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }

        public void UpdatePassword(VcUsers entity)
        {
            entity.Password = this.NewPassword;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public record UserFilterModel : BaseQueryFilterModel
    {
        public string? Role { get; set; }
    }
}
