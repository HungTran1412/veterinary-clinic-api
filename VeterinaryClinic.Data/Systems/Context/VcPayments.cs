using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data
{
    /// <summary>
    /// Bang thanh toan
    /// </summary>
    [Table("vcPayments")]
    public class VcPayments : BaseEntity
    {
        /// <summary>
        /// ID của hóa đơn tổng (nếu có).
        /// Dùng cho các khoản thanh toán gộp nhiều dịch vụ.
        /// </summary>
        [Column("bill_id")]
        public int? BillId { get; set; }

        /// <summary>
        /// Id hóa đơn dịch vụ (dùng cho logic cũ, hoặc thanh toán riêng lẻ).
        /// Có thể null nếu thanh toán theo hóa đơn tổng.
        /// </summary>
        [Column("invoice_id")]
        public int? InvoiceId { get; set; }

        /// <summary>
        /// Mã thanh toán nội bộ
        /// </summary>
        [Column("code"), MaxLength(50)]
        public string Code { get; set; }

        /// <summary>
        /// Cash | VNPay
        /// </summary>
        [Column("payment_method"), MaxLength(50)]
        public string PaymentMethod { get; set; }

        /// <summary>
        /// Pending | Success | Failed
        /// </summary>
        [Column("payment_status"), MaxLength(50)]
        public string PaymentStatus { get; set; }

        /// <summary>
        /// Số tiền thanh toán
        /// </summary>
        [Column("amount", TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Mã giao dịch VNPay
        /// Tiền mặt sẽ null
        /// </summary>
        [Column("gateway_transaction_id"), MaxLength(100)]
        public string? GatewayTransactionId { get; set; }

        /// <summary>
        /// Mã phản hồi VNPay
        /// Tiền mặt sẽ null
        /// </summary>
        [Column("response_code"), MaxLength(10)]
        public string? ResponseCode { get; set; }

        /// <summary>
        /// Raw response VNPay
        /// Tiền mặt sẽ null
        /// </summary>
        [Column("gateway_response"), MaxLength(10000)]
        public string? GatewayResponse { get; set; }

        /// <summary>
        /// Thời gian thanh toán
        /// </summary>
        [Column("payment_date")]
        public DateTime? PaymentDate { get; set; }
    } 
}
