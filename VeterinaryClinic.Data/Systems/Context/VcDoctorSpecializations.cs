using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang chuyen nganh cua bac si
    /// </summary>
    [Table("vcDoctorSpecializations")]
    public class VcDoctorSpecializations : BaseEntity
    {
        public VcDoctorSpecializations()
        {
        }
        
        /// <summary>
        /// id bac si, FK → Users(id)
        /// </summary>
        [Column("doctor_id")]
        public int DoctorId { get; set; }
        
        /// <summary>
        /// id chuyen nganh, FK → Specializations(id)
        /// </summary>
        [Column("specialization_id")]
        public int SpecializationId { get; set; }
    }
}