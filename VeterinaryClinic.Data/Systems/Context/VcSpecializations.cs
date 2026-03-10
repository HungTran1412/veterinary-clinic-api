using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang chuyen nganh
    /// </summary>
    [Table("vcSpecializations")]
    public class VcSpecializations : BaseEntity
    {
        public VcSpecializations()
        {
        }
        
        /// <summary>
        /// Ten chuyen nganh
        /// </summary>
        [Column("name"), MaxLength(150)]
        public string Name { get; set; }
        
        /// <summary>
        /// Mo ta
        /// </summary>
        [Column("description"), MaxLength(1000)]
        public string Description { get; set; }
    }   
}