using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang dang ky lich lam viec
    /// </summary>
    [Table("vcWorkScheduleRegistrations")]
    public class VcWorkScheduleRegistrations : BaseEntity
    {
        public VcWorkScheduleRegistrations()
        {
        }
        
        /// <summary>
        /// Mã định danh (tự sinh hoặc theo quy tắc)
        /// </summary>
        [Column("code", TypeName = "varchar(20)")]
        public string Code { get; set; }
        
        /// <summary>
        /// id nguoi dung
        /// </summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// id ca lam viec
        /// </summary>
        [Column("shift_template_id")]
        public int ShiftTemplateId { get; set; }
        
        /// <summary>
        /// Ngay dang ky
        /// </summary>
        [Column("work_date")]
        public DateTime WorkDate { get; set; }
        
        /// <summary>
        /// trang thai
        /// </summary>
        [Column("status", TypeName = "varchar(10)")]
        public string Status { get; set; }
    
        /// <summary>
        /// ngay dang ky
        /// </summary>
        [Column("registered_date")]
        public DateTime RegisteredDate { get; set; }
        
        /// <summary>
        /// ghi chu
        /// </summary>
        [Column("note"), MaxLength(1000)]
        public string Note { get; set; }
    
    }   
}