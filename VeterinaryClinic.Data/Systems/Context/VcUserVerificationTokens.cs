using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bamg token xac thuc
    /// </summary>
    [Table("vcUserVerificationTokens")]
    public class VcUserVerificationTokens : BaseEntity
    {
        public VcUserVerificationTokens()
        {
        }
        
        /// <summary>
        /// Mã định danh (tự sinh hoặc theo quy tắc)
        /// </summary>
        [Column("code"), MaxLength(100)]
        public string Code { get; set; }
        
        /// <summary>
        /// id nguoi dung
        /// </summary>
        [Column("user_id")]
        public int UserId { get; set; }
        
        /// <summary>
        /// ma xac thuc
        /// </summary>
        [Column("token"), MaxLength(255)]
        public string Token { get; set; }
        
        /// <summary>
        /// loai token
        /// </summary>
        [Column("token_type"), MaxLength(50)]
        public string TokenType { get; set; }
        
        /// <summary>
        /// thoi gian het han
        /// </summary>
        [Column("expiration_at")]
        public DateTime ExpirationAt { get; set; }
        
        /// <summary>
        /// trang thai su dung
        /// </summary>
        [Column("is_used")]
        public bool IsUsed { get; set; } = false;
        
        /// <summary>
        /// ngay su dung
        /// </summary>
        [Column("used_date")]
        public DateTime? UsedDate { get; set; }
    }
}
