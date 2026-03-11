using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang ho so kham
    /// </summary>
    [Table("vcMedicalRecords")]
    public class vcMedicalRecords: BaseEntity
    {
        public vcMedicalRecords()
        {
        }
        
        /// <summary>
        /// Id lich kham, FK → Appointments(id)
        /// </summary>
        [Column("appointment_id")]
        public int AppointmentId { get; set; }
        
        /// <summary>
        /// Id bac si, FK → Users(id)
        /// </summary>
        [Column("doctor_id")]
        public int DoctorId { get; set; }
        
        /// <summary>
        /// Mã định danh (tự sinh hoặc theo quy tắc)
        /// </summary>
        [Column("code"), MaxLength(100)]
        public string Code { get; set; }
        
        /// <summary>
        /// Trieu chung
        /// </summary>
        [Column("symptoms")]
        public string Symptoms { get; set; }
        
        /// <summary>
        /// Chan doan
        /// </summary>
        [Column("diagnosis")]
        public string Diagnosis { get; set; }
        
        /// <summary>
        /// Phac do dieu tri
        /// </summary>
        [Column("treatment_plan")]
        public string TreatmentPlan { get; set; }
        
        /// <summary>
        /// Don thuoc
        /// </summary>
        [Column("prescription")]
        public string Prescription { get; set; }
        
        /// <summary>
        /// Ghi chu cua bac si
        /// </summary>
        [Column("doctor_note")]
        public string DoctorNote { get; set; }
    
        /// <summary>
        /// Ngay hoan thanh
        /// </summary>
        [Column("completed_date")]
        public DateTime? CompletedDate { get; set; }
    
    }   
}