using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang thong bao
    /// </summary>
    [Table("vcNotifications")]
    public class VcNotifications: BaseEntity
    {
        public VcNotifications()
        {
        }
        
        /// <summary>
        /// Nguoi nhan thong bao
        /// </summary>
        [Column("user_id")]
        public int UserId { get; set; }
        
        /// <summary>
        /// Mã định danh (tự sinh hoặc theo quy tắc)
        /// </summary>
        [Column("code"), MaxLength(100)]
        public string Code { get; set; }
        
        /// <summary>
        /// Tieu de thong bao
        /// </summary>
        [Column("title"), MaxLength(200)]
        public string Title { get; set; }

        /// <summary>
        /// loai thong bao
        /// </summary>
        [Column("type"), MaxLength(100)]
        public string Type { get; set; }

        /// <summary>
        /// da doc chua
        /// </summary>
        [Column("is_read")]
        public bool IsRead { get; set; } = false;
        
        /// <summary>
        /// Id dữ liệu liên quan
        /// </summary>
        [Column("related_entity_id")]
        public int RelatedEntityId { get; set; }
        
        /// <summary>
        /// Loại dữ liệu liên quan
        /// </summary>
        [Column("related_entity_type")]
        public int RelatedEntityType { get; set; }
    }   
}