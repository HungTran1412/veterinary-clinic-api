namespace VeterinaryClinic.Shared
{
    public class MailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string ApiBaseUrl { get; set; }
        public string FrontendBaseUrl { get; set; }
    }
}
