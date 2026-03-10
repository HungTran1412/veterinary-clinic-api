using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace VeterinaryClinic.Shared.ContextAccessor
{
    public class HttpContextAccessorWrapper : IContextAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _traceId;

        public HttpContextAccessorWrapper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _traceId = Guid.NewGuid().ToString();
        }

        public string CorrelationId
        {
            get
            {
                var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].ToString();
                return string.IsNullOrEmpty(correlationId) ? _traceId : correlationId;
            }
        }

        public string TraceId => _traceId;

        public int? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
                return null;
            }
        }

        public string UserName
        {
            get => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
        }

        public string Role
        {
            get => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        }

        public string Language
        {
            get
            {
                var language = _httpContextAccessor.HttpContext?.Request.Headers["Accept-Language"].ToString();
                return string.IsNullOrEmpty(language) ? "vi-VN" : language;
            }
        }
    }
}