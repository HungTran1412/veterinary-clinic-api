using System.ComponentModel.DataAnnotations;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class UserBaseModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "user.code.required")]
        public string Code { get; set; }
        
        [Required(ErrorMessage = "user.username.required")]
        [MaxLength(100)]
        public string UserName { get; set; }
        
        [Required(ErrorMessage = "user.email.required")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "user.password.required")]
        public string Password { get; set; }
        
        [Required(ErrorMessage = "user.full_name.required")]
        public string FullName { get; set; }
        
        [Required(ErrorMessage = "user.phone_number.required")]
        public string PhoneNumber { get; set; }
        
        public string AvatarUrl { get; set; }
        
        [Required(ErrorMessage = "user.role.required")]
        public string Role { get; set; }
        
        public bool IsActive { get; set; } = true;

        public int Order { get; set; }
        public DateTime? CreatedDate { get; set; }
        
    }
    public class UserModel : UserBaseModel
    {
    
    }
    
    public class CreateUserModel : UserModel
    {
        public int? CreatedUserId { get; set; }
    }

    public class UpdateUserModel : UserModel
    {
        public int? ModifiedUserId { get; set; }

        public void UpdateEntity(VcUsers entity)
        {
            entity.Email = this.Email;
            entity.FullName = this.FullName;
            entity.PhoneNumber = this.PhoneNumber;
            entity.AvatarUrl = this.AvatarUrl;
            entity.Order = this.Order;
            entity.ModifiedUserId = entity.ModifiedUserId;
        }
    }

    public class UserSelectItemModel : SelectItemModel
    {
        
    }

    public class UserFilterModel : BaseQueryFilterModel
    {
        public string Code { get; set; }
        public string FullName { get; set; }
        public string Email { set; get; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
    }
}
