using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang dich vu
    /// </summary>
    [Table("vcServices")]
    public class VcServices: BaseEntity
    {
        public VcServices()
        {
        }
        
        /// <summary>
        /// Ten dich vu
        /// </summary>
        [Column("name"), MaxLength(150)]
        public string Name { get; set; }
        
        /// <summary>
        /// Gia dich vu
        /// </summary>
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        
        /// <summary>
        /// thoi gian thuc hien
        /// </summary>
        [Column("duration_minutes")]
        public int DurationMinutes { get; set; }
        
        /// <summary>
        /// Chuyen nganh phuc vu
        /// </summary>
        [Column("specialization_id")]
        public int SpecializationId { get; set; }
        
        
        /// <summary>
        /// anh dich vu
        /// </summary>
        [Column("ImageUrl"), MaxLength(500)]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Trang thai cung cap dich vu
        /// </summary>
        [Column("is_available")]
        public bool IsAvailable { get; set; } = true;
        
        /// <summary>
        /// Mo ta
        /// </summary>
        [Column("description")]
        public string Description { get; set; }
    }   
}