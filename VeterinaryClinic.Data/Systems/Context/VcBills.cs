using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bảng hóa đơn tổng, đại diện cho một lần thanh toán của khách hàng cho nhiều dịch vụ.
    /// </summary>
    [Table("vcBills")]
    public class VcBills : BaseEntity
    {
        public VcBills()
        {
        }

        /// <summary>
        /// Mã định danh của hóa đơn tổng (tự sinh hoặc theo quy tắc)
        /// </summary>
        [Column("code"), MaxLength(100)]
        public string Code { get; set; }

        /// <summary>
        /// ID của khách hàng thanh toán
        /// </summary>
        [Column("customer_id")]
        public int CustomerId { get; set; }

        /// <summary>
        /// Tổng số tiền của tất cả các hóa đơn dịch vụ con
        /// </summary>
        [Column("total_amount", TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Trạng thái của hóa đơn tổng (PENDING, PAID, CANCELLED)
        /// </summary>
        [Column("status"), MaxLength(50)]
        public string Status { get; set; }

        /// <summary>
        /// Ngày tạo hóa đơn tổng
        /// </summary>
        [Column("bill_date")]
        public DateTime BillDate { get; set; }
        
        /// <summary>
        /// Ghi chú cho hóa đơn tổng
        /// </summary>
        [Column("note")]
        public string? Note { get; set; }
    }
}
