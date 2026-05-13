using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Business
{
    public abstract record PaymentBaseModel
    {
        public int Id { get; init; }
        
        [Required(ErrorMessage = "payment.code.required")]
        public string Code { get; init; }
      
        [Required(ErrorMessage = "payment.payment_method.required")]
        public string PaymentMethod { get; init; }
        
        [Required(ErrorMessage = "payment.payment_status.required")]
        public string PaymentStatus { get; init; }
          
        [Required(ErrorMessage = "payment.amount.required")]
        public decimal Amount { get; init; }
        
        public string? GatewayTransactionId { get; init; }

        public string? ResponseCode { get; init; }

        public string? GatewayResponse { get; init; }

        public DateTime? PaymentDate { get; init; }
        
        public bool IsActive { get; init; } = true;

        public int Order { get; init; }
        public DateTime? CreatedDate { get; init; }
    }

    public record PaymentModel : PaymentBaseModel
    {
    }

    public record CreatePaymentModel : PaymentModel
    {
        public int? CreatedUserId { get; init; }
    }
}