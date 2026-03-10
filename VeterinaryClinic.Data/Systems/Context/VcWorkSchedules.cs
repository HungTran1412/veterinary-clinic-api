using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang lich lam viec
    /// </summary>
    [Table("vcWorkSchedules")]
    public class VcWorkSchedules : BaseEntity
    {
        public VcWorkSchedules()
        {
        }
        
        /// <summary>
        /// id bac si, le tan. FK → Users(id)
        /// </summary>
        [Column("user_id")]
        public int UserId { get; set; }
        
        /// <summary>
        /// Ngay lam viec
        /// </summary>
        [Column("work_date")]
        public DateTime WorkDate { get; set; }
        
        /// <summary>
        ///  Thoi gian bat dau ca
        /// </summary>
        [Column("start_time")]
        public DateTime StartTime { get; set; }
        
        /// <summary>
        ///  Thoi gian ket thuc ca
        /// </summary>
        [Column("end_time")]
        public DateTime EndTime { get; set; }
    
        /// <summary>
        ///  Ten ca lam viec
        /// </summary>
        [Column("shift_name")]
        public string ShiftName { get; set; }
        
        /// <summary>
        /// Ghi chu
        /// </summary>
        [Column("note")]
        public string Note { get; set; }
    }

}