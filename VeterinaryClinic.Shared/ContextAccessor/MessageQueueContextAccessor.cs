using System.Threading;

namespace VeterinaryClinic.Shared.ContextAccessor
{
    /// <summary>
    /// Cung cấp context cho các tác vụ chạy nền (background tasks), message queues, 
    /// hoặc bất kỳ tiến trình nào chạy ngoài pipeline của một HTTP request.
    /// Nó sử dụng AsyncLocal để duy trì context qua các lời gọi bất đồng bộ.
    /// </summary>
    public class MessageQueueContextAccessor : IContextAccessor
    {
        private static readonly AsyncLocal<MessageQueueContext> _context = new();

        /// <summary>
        /// Phương thức này cho phép một message handler thiết lập toàn bộ context ngay từ đầu.
        /// </summary>
        /// <param name="context">Context được truyền từ message</param>
        public static void SetContext(MessageQueueContext context)
        {
            _context.Value = context;
        }

        public string CorrelationId => _context.Value?.CorrelationId;
        public string TraceId => _context.Value?.TraceId;
        public int? UserId => _context.Value?.UserId;
        public string UserName => _context.Value?.UserName;
        public string Role => _context.Value?.Role;
        public string Language => _context.Value?.Language;

        /// <summary>
        /// Đối tượng chứa thông tin context.
        /// Thường được deserialize từ header của một message trong hàng đợi.
        /// </summary>
        public class MessageQueueContext
        {
            public string CorrelationId { get; set; }
            public string TraceId { get; set; }
            public int? UserId { get; set; }
            public string UserName { get; set; }
            public string Role { get; set; }
            public string Language { get; set; }
        }
    }
}