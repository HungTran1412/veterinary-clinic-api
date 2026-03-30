using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Business
{
    public class EmailLogBaseModel
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

    public class EmailLogModel : EmailLogBaseModel
    {
        public string Body { set; get; }
    }

    public class EmailLogFilterModel : BaseQueryFilterModel
    {
        public string? Subject { set; get; }
    }
}