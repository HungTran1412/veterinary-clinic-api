using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.Business;
using VeterinaryClinic.Shared;
using VeterinaryClinic.Shared.ContextAccessor;

namespace VeterinaryClinic.API.Controllers
{
    /// <summary>
    /// Module cache management
    /// </summary>
    [ApiController]
    [Route("veterinary-clinic/v1/cache")]
    [ApiExplorerSettings(GroupName = "100. Cache Service", IgnoreApi = false)]
    [AllowAnonymous]
    public class CacheController : ApiControllerBase
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ICacheService _cacheService;

        public CacheController(IDistributedCache distributedCache, ICacheService cacheService)
        {
            _distributedCache = distributedCache;
            _cacheService = cacheService; 
        }

        /// <summary>
        /// Remove all cache
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("clean-all-cache")]
        public string CleanAllCache()
        {
            _cacheService.RemoveAll();
            return "complete";
        }

        /// <summary>
        /// Remove all cache by prefix
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("clean-cache-by-prefix")]
        public string CleanCacheByPrefix(string prefix)
        {
            _cacheService.RemoveAllWithPrefix(prefix);
            return "complete";
        }
    }   
}