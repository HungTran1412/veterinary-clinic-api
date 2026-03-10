using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang lich kham
    /// </summary>
    [Table("vcAppointments")]
    public class VcAppointments: BaseWorkflowEntity
    {
        public VcAppointments()
        {
        }
        
        /// <summary>
        /// id khach hang dat lich
        /// </summary>
        [Column("customer_id")]
        public int CustomerId { get; set; }
        
        /// <summary>
        /// Mã định danh (tự sinh hoặc theo quy tắc)
        /// </summary>
        [Column("code"), MaxLength(100)]
        public string Code { get; set; }
        
        /// <summary>
        /// id thu cung
        /// </summary>
        [Column("pet_id")]
        public int PetId { get; set; }
        
        /// <summary>
        /// id dich vu
        /// </summary>
        [Column("service_id")]
        public int SerivceId { get; set; }
        
        /// <summary>
        /// id bac si
        /// </summary>
        [Column("doctor_id")]
        public int DoctorId { get; set; }
        
        /// <summary>
        /// Ngay kham
        /// </summary>
        [Column("appointment_date")]
        public DateTime AppointmentDate { get; set; }
        
        /// <summary>
        /// Thoi gian bat dau
        /// </summary>
        [Column("start_time")]
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Thoi gian bat dau
        /// </summary>
        [Column("end_time")]
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// Ly do huy
        /// </summary>
        [Column("cancel_reason"), MaxLength(500)]
        public string CancelReason { get; set; }

        /// <summary>
        /// Ghi chu
        /// </summary>
        [Column("note")]
        public string Note { get; set; }
    }   
}