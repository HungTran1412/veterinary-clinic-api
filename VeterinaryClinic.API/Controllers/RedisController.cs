using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Serilog;
using VeterinaryClinic.Business.Core;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.API.Controllers;

/// <summary>
/// Module test redis
/// </summary>
[ApiController]
[Route("veterinary-clinic/v1/redis")]
[ApiExplorerSettings(GroupName = "97. Redis-test", IgnoreApi = false)]
[AllowAnonymous]
public class RedisController : ApiControllerBase
{
    private readonly IRedisHandler _redisHandler;
    public RedisController(Func<IContextAccessor> contextAccessorFactory, IRedisHandler redisHandler, IMediator mediator, IStringLocalizer<Resources> localizer, IConfiguration config) : base(contextAccessorFactory, mediator, localizer, config)
    {
        _redisHandler = redisHandler;
    }

    /// <summary>
    /// Test redis connect using IRedisHandler
    /// </summary>
    /// <returns></returns>
    /// <response code="200">Returns the current time and cached time</response>
    [HttpGet, Route("cache")]
    public async Task<string> Get()
    {
        const string cacheKey = "TheTime";
        var currentTime = DateTime.Now.ToString("O");
        var cachedTime = await _redisHandler.GetAsync(cacheKey);

        if (string.IsNullOrEmpty(cachedTime))
        {
            // Cache expire in 300s
            await _redisHandler.SetAsync(cacheKey, currentTime, TimeSpan.FromSeconds(300));
            cachedTime = await _redisHandler.GetAsync(cacheKey);
        }

        var netCoreVer = Environment.Version;
        var runtimeVer = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

        Log.Information($"NetCore version: {netCoreVer}");
        Log.Information($"Runtime version: {runtimeVer}");
        Log.Information($"{_localizer["redis.test.label"]}");

        string result = $"{_localizer["redis.test.label"]}" +
            $"\n{_localizer["redis.test.current-time"]} : {currentTime} " +
            $"\n{_localizer["redis.test.cache-time"]} : {cachedTime}";
        return result;
    }

    /// <summary>
    /// Delete a key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [HttpDelete, Route("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsync(string key)
    {
        return await ExecuteFunction(async () =>
        {
            return await _redisHandler.DeleteAsync(key);
        });
    }

    /// <summary>
    /// Delete a hash field
    /// </summary>
    /// <param name="key"></param>
    /// <param name="hashKey"></param>
    /// <returns></returns>
    [HttpDelete, Route("{key}/{hashKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteHashAsync(string key, string hashKey)
    {
        return await ExecuteFunction(async () =>
        {
            return await _redisHandler.DeleteHashAsync(key, hashKey);
        });
    }

    #region Increase - Decrease
    /// <summary>
    /// Set a long value
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost, Route("set-long")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetLongAsync([FromBody] RedisInCreaseModel model)
    {
        return await ExecuteFunction(async () =>
        {
            return await _redisHandler.SetLongValueAsync(model.Key, model.Value);
        });
    }

    /// <summary>
    /// Increment a value
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [HttpPost, Route("increment/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> IncrementAsync([FromRoute] string key)
    {
        return await ExecuteFunction(async () =>
        {
            return await _redisHandler.StringIncrementAsync(key);
        });
    }

    /// <summary>
    /// Decrement a value
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [HttpPost, Route("decrement/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DecrementAsync([FromRoute] string key)
    {
        return await ExecuteFunction(async () =>
        {
            return await _redisHandler.StringDecrementAsync(key);
        });
    }

    #endregion

    /// <summary>
    /// Set a string value
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost, Route("set")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetAsync([FromBody] RedisModel model)
    {
        return await ExecuteFunction(async () =>
        {
            return await _redisHandler.SetAsync(model.Key, model.Value);
        });
    }

    /// <summary>
    /// Set a string value with expiration
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost, Route("set-with-expire")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetWithExpireAsync([FromBody] RedisModel model)
    {
        return await ExecuteFunction(async () =>
        {
            return await _redisHandler.SetAsync(model.Key, model.Value, TimeSpan.FromSeconds(model.Second));
        });
    }

    /// <summary>
    /// Get a string value
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [HttpGet, Route("get/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(string key)
    {
        return await ExecuteFunction(async () =>
        {
            return await _redisHandler.GetAsync(key);
        });
    }
}
