using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang thu cung
    /// </summary>
    [Table("vcPets")]
    public class VcPets : BaseEntity
    {
        public VcPets()
        {
        }
        
        /// <summary>
        /// Mã định danh (tự sinh hoặc theo quy tắc)
        /// </summary>
        [Column("code"), MaxLength(100)]
        public string Code { get; set; }
        
        /// <summary>
        /// Ten thu cung
        /// </summary>
        [Column("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// Loai: cho, meo ...
        /// </summary>
        [Column("species")]
        public string Species { get; set; }
        
        
        /// <summary>
        /// Giong: anh long ngan...
        /// </summary>
        [Column("breed")]
        public string? Breed { get; set; }
        
        /// <summary>
        /// gioi tinh
        /// </summary>
        [Column("gender")]
        public bool? Gender { get; set; }

        /// <summary>
        /// trang thai triet san
        /// </summary>
        [Column("is_neutered")]
        public bool IsNeutered { get; set; } = false;
        
        /// <summary>
        /// ngay sinh
        /// </summary>
        [Column("birth_date")]
        public DateTime? BirthDate { get; set; }
        
        /// <summary>
        /// Can nang thu cung
        /// </summary>
        [Column("weight")]
        public double? Weight { get; set; }
        
        /// <summary>
        /// mau long
        /// </summary>
        [Column("color"), MaxLength(50)]
        public string? Color { get; set; }
        
        /// <summary>
        /// Duong dan anh
        /// </summary>
        [Column("image_url"), MaxLength(500)]
        public string? ImageUrl { get; set; }
        
        /// <summary>
        /// Chu so huu, FK → Users(id)
        /// </summary>
        [Column("owner_id")]
        public int OwnerId { get; set; }
        
        /// <summary>
        /// Ghi chu
        /// </summary>
        [Column("note")]
        public string? Note { get; set; }
    }   
}