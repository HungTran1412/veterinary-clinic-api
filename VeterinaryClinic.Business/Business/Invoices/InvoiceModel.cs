using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Business
{
    public abstract record InvoiceBaseModel
    {
        public int Id { get; init; }
        
        [Required(ErrorMessage = "invoice.appointment_id.required")]
        public int AppointmentId { get; init; }
      
        [Required(ErrorMessage = "invoice.code.required")]
        public string Code { get; init; }
        
        [Required(ErrorMessage = "invoice.status.required")]
        public string Status { get; init; }
          
        [Required(ErrorMessage = "invoice.total_amount.required")]
        public decimal TotalAmount { get; init; }
       
        public DateTime PaidDate { get; init; }
        
        public bool IsActive { get; init; } = true;

        public int Order { get; init; }
        public DateTime? CreatedDate { get; init; }
    }
    
    public record InvoiceModel : InvoiceBaseModel
    {
    }

    public record CreateInvoiceModel : InvoiceBaseModel
    {
        public int? CreatedUserId { get; init; }
    }
}