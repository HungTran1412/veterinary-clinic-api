using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang hoa don
    /// </summary>
    [Table("vcInvoices")]
    public class VcInvoices: BaseEntity
    {
        public VcInvoices()
        {
        }
        
        /// <summary>
        /// Id lich kham
        /// </summary>
        [Column("appointment_id")]
        public int AppointmentId { get; set; }
        
        /// <summary>
        /// Mã định danh (tự sinh hoặc theo quy tắc)
        /// </summary>
        [Column("code"), MaxLength(100)]
        public string Code { get; set; }
        
        /// <summary>
        /// Tong tien
        /// </summary>
        [Column("total_amount", TypeName = "decimal(12,2)")]
        public decimal TotalAmount { get; set; }
        
        /// <summary>
        /// Trang thai hoa don
        /// </summary>
        [Column("status"), MaxLength(50)]
        public string Status { get; set; }
        
        /// <summary>
        /// Ngay thanh toan
        /// </summary>
        [Column("paid_date")]
        public DateTime PaidDate { set; get; }
    }   
}