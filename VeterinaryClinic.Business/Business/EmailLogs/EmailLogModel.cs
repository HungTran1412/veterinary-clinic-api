using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Business
{
    public abstract record EmailLogBaseModel
    {
        [Required(ErrorMessage = "service.emailog-id.required")]
        public int Id { get; init; }
        [Required(ErrorMessage = "service.emailog-code.required")]
        public string Code { get; init; }
        [Required(ErrorMessage = "service.emailog-to-email.required")]
        public string ToEmail { get; init; }
        [Required(ErrorMessage = "service.emailog-subject.required")]
        public string Subject { get; init; }
        public string SentStatus { init; get; }
        public string ErrorMessage { get; init; } 
    }

    public record EmailLogModel : EmailLogBaseModel
    {
        
    }

    public record InfoEmailLogModel : EmailLogModel
    {
        public string Body { init; get; }
    }
    
    public record EmailLogFilterModel : BaseQueryFilterModel
    {
        public string? Subject { init; get; }
    }
}