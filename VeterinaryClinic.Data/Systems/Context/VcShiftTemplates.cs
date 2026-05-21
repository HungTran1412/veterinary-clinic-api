using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
 
    /// <summary>
    /// Bang ca lam viec
    /// </summary>
    [Table("vcShiftTemplates")]
    public class VcShiftTemplates: BaseEntity
    {
        public VcShiftTemplates()
        {
        }
        
        /// <summary>
        /// Mã định danh (tự sinh hoặc theo quy tắc)
        /// </summary>
        [Column("code", TypeName = "varchar(20)"), MaxLength(100)]
        public string Code { get; set; }
        
        /// <summary>
        /// Ten ca lam viec
        /// </summary>
        [Column("shift_name")]
        public string ShiftName { get; set; }
        
        /// <summary>
        /// gio bat dau
        /// </summary>
        [Column("start_time")]
        public TimeOnly StartTime { get; set; }
        
        /// <summary>
        /// gio ket thuc
        /// </summary>
        [Column("end_time")]
        public TimeOnly EndTime { get; set; }

        /// <summary>
        /// so luong nhan vien
        /// </summary>
        [Column("max_employee")]
        public int MaxEmployee { get; set; } = 15;
    }   
}