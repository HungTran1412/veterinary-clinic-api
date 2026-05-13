using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang thong bao
    /// </summary>
    [Table("vcPayments")]
    public class VcPayments : BaseEntity
    {
        /// <summary>
        /// Id hóa đơn
        /// </summary>
        public int InvoiceId { get; set; }

        /// <summary>
        /// Mã thanh toán nội bộ
        /// </summary>
        [MaxLength(50)]
        public string Code { get; set; }

        /// <summary>
        /// Cash | VNPay
        /// </summary>
        public string PaymentMethod { get; set; }

        /// <summary>
        /// Pending | Success | Failed
        /// </summary>
        public string PaymentStatus { get; set; }

        /// <summary>
        /// Số tiền thanh toán
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Mã giao dịch VNPay
        /// Tiền mặt sẽ null
        /// </summary>
        [MaxLength(100)]
        public string? GatewayTransactionId { get; set; }

        /// <summary>
        /// Mã phản hồi VNPay
        /// Tiền mặt sẽ null
        /// </summary>
        [MaxLength(10)]
        public string? ResponseCode { get; set; }

        /// <summary>
        /// Raw response VNPay
        /// Tiền mặt sẽ null
        /// </summary>
        public string? GatewayResponse { get; set; }

        /// <summary>
        /// Thời gian thanh toán
        /// </summary>
        public DateTime? PaymentDate { get; set; }
    } 
}