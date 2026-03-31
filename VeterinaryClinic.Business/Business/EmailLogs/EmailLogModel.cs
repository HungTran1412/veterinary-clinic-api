using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Business
{
    public record EmailLogBaseModel
    {
        [Required(ErrorMessage = "service.emailog-id.required")]
        public int Id { get; set; }
        [Required(ErrorMessage = "service.emailog-code.required")]
        public string Code { get; set; }
        [Required(ErrorMessage = "service.emailog-to-email.required")]
        public string ToEmail { get; set; }
        [Required(ErrorMessage = "service.emailog-subject.required")]
        public string Subject { get; set; }
        public string SentStatus { set; get; }
        public string ErrorMessage { get; set; } 
    }

    public record EmailLogModel : EmailLogBaseModel
    {
        public string Body { set; get; }
    }

    public record EmailLogFilterModel : BaseQueryFilterModel
    {
        public string? Subject { set; get; }
    }
}