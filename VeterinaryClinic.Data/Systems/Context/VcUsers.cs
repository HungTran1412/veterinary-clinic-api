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
        /// Mã định danh (tự sinh hoặc theo quy tắc)
        /// </summary>
        [Column("code", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Code { get; set; }
        
        /// <summary>
        /// Ten dang nhap
        /// </summary>
        [Column("username", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Username { get; set; }
        
        /// <summary>
        /// email
        /// </summary>
        [Column("email", TypeName = "varchar(255)")]
        public string Email { get; set; }
        
        /// <summary>
        /// mat khau
        /// </summary>
        [Column("password", TypeName = "varchar(255)")]
        public string Password { get; set; }
        
        /// <summary>
        /// Ho ten
        /// </summary>
        [Column("full_name")]
        public string FullName { get; set; }
        
        /// <summary>
        /// So dien thoai
        /// </summary>
        [Column("phone_number", TypeName = "varchar(20)")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }
        
        /// <summary>
        /// Gioi tinh
        /// </summary>
        [Column("gender")]
        public int? Gender { get; set; }
        
        /// <summary>
        /// Duong dan anh
        /// </summary>
        [Column("avatar_url")]
        public string AvatarUrl { get; set; }
    
        /// <summary>
        /// Vai tro: ADMIN, DOCTOR, RECEPTIONIST, CUSTOMER
        /// </summary>
        [Column("role", TypeName = "varchar(50)")]
        public string Role { get; set; }

        /// <summary>
        /// Địa chỉ
        /// </summary>
        [Column("address")]
        public string Address { get; set; }
        
        /// <summary>
        /// Refresh Token để cấp lại Access Token
        /// </summary>
        [Column("refresh_token")]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Thời gian hết hạn của Refresh Token
        /// </summary>
        [Column("refresh_token_expiry_time")]
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }   
}