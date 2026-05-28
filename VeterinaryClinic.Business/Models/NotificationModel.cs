namespace VeterinaryClinic.Business.Models
{
    public class NotificationModel
    {
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; } // This will be the content sent via SignalR
        public string Type { get; set; }
        public int? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
    }
}
