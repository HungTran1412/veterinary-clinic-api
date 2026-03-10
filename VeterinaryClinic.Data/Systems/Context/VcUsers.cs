using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang nguoi dung
    /// </summary>
    [Table("vcUsers")]
    public class VcUsers : BaseEntity
    {
        public VcUsers()
        {
        }
        
        /// <summary>
        /// Ten dang nhap
        /// </summary>
        [Column("username"), MaxLength(100)]
        public string Username { get; set; }
        
        /// <summary>
        /// email
        /// </summary>
        [Column("email")]
        public string Email { get; set; }
        
        /// <summary>
        /// mat khau
        /// </summary>
        [Column("password")]
        public string Password { get; set; }
        
        /// <summary>
        /// Ho ten
        /// </summary>
        [Column("full_name")]
        public string FullName { get; set; }
        
        /// <summary>
        /// So dien thoai
        /// </summary>
        [Column("phone_number"), MaxLength(10)]
        public string PhoneNumber { get; set; }
        
        /// <summary>
        /// Duong dan anh
        /// </summary>
        [Column("avatar_url")]
        public string AvatarUrl { get; set; }
    
        /// <summary>
        /// Vai tro: ADMIN, DOCTOR, RECEPTIONIST, CUSTOMER
        /// </summary>
        [Column("role")]
        public string Role { get; set; }
    }   
}