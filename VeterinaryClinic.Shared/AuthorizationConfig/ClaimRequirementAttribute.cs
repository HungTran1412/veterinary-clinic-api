using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VeterinaryClinic.Shared
{
    public class ClaimRequirementAttribute : TypeFilterAttribute
    {
        public ClaimRequirementAttribute(string claimType, string claimValue) : base(typeof(ClaimRequirementFilter))
        {
            Arguments = new object[] { new Claim(claimType, claimValue) };
        }
    }

    public class ClaimRequirementFilter : IAuthorizationFilter
    {
        readonly Claim _claim;

        public ClaimRequirementFilter(Claim claim)
        {
            _claim = claim;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Tách cách claim khác nhau, phân tách với nhau bởi dấu ,
            string[] claims = _claim.Value.Split(',');
            // Kiểm tra theo điều kiện "hoặc", người dùng chỉ cần có 1 trong các quyền thì sẽ cho truy cập nghiệp vụ
            var requestClaims = context.HttpContext.User.Claims.FirstOrDefault(c => c.Type == _claim.Type)?.Value;
            if (!string.IsNullOrEmpty(requestClaims))
            {
                var permissions = requestClaims.Split(',');
                if (permissions.Any(c => claims.Contains(c)))
                    return;
            }
            context.Result = new ForbidResult();
        }
    }
  
}
