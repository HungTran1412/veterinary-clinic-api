namespace VeterinaryClinic.Shared
{
    public class VnPaySettings
    {
        public string TmnCode { get; set; }
        public string HashSecret { get; set; }
        public string BaseUrl { get; set; }
        public string PayUrl { get; set; } // Giữ lại PayUrl nếu bạn có ý định dùng nó
        public string ReturnUrl { get; set; }
        public string FrontendReturnUrl { get; set; } // Thêm thuộc tính mới
        public string Command { get; set; }
        public string CurrCode { get; set; }
        public string Version { get; set; }
        public string Locale { get; set; }
        public int ExpireMinutes { get; set; } = 15;
    }
}
