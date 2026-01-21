using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Serilog;
using StackExchange.Redis;

namespace VeterinaryClinic.Business
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConfiguration _config;

        // Cache expire trong 300s
        private int _timeLive = 300;
        private string? _redisConnectionString = "";

        public RedisCacheService(IDistributedCache distributedCache, IConfiguration config)
        {
            _config = config;
            _distributedCache = distributedCache;
            int.TryParse(_config["redis:timeLive"], out _timeLive);
            if (_timeLive <= 0)
            {
                _timeLive = 300;
            }

            _redisConnectionString = _config["redis:configuration"];
        }

        public async Task<TItem?> GetOrCreate<TItem>(string key, Func<Task<TItem>> createItem,
            DistributedCacheEntryOptions? options = null)
        {
            if (_config["AppSettings:EnableCache"] == "true")
            {
                TItem? cacheEntry;
                try
                {
                    var itemString = await _distributedCache.GetStringAsync(key);
                    if (string.IsNullOrEmpty(itemString))
                    {
                        // Cache expire trong 300s
                        if (options == null)
                            options = new DistributedCacheEntryOptions().SetSlidingExpiration(
                                TimeSpan.FromSeconds(_timeLive));

                        cacheEntry = await createItem();
                        var item = JsonSerializer.Serialize(cacheEntry);

                        // Nạp lại giá trị mới cho cache
                        await _distributedCache.SetStringAsync(key, item, options);
                    }
                    else
                    {
                        cacheEntry = JsonSerializer.Deserialize<TItem>(itemString);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Có lỗi xảy ra khi xử lý dữ liệu từ redis - {ex.ToString()}");
                    cacheEntry = await createItem();
                }

                return cacheEntry;
            }
            else
            {
                return await createItem();
            }
        }

        public async Task<TItem?> GetOrCreate<TItem>(string key, Func<TItem> createItem,
            DistributedCacheEntryOptions? options = null)
        {
            if (_config["AppSettings:EnableCache"] == "true")
            {
                TItem? cacheEntry;
                try
                {
                    var itemString = await _distributedCache.GetStringAsync(key);
                    if (string.IsNullOrEmpty(itemString))
                    {
                        // Cache expire trong 300s
                        if (options == null)
                            options = new DistributedCacheEntryOptions().SetSlidingExpiration(
                                TimeSpan.FromSeconds(_timeLive));

                        cacheEntry = createItem();
                        var item = JsonSerializer.Serialize(cacheEntry);

                        // Nạp lại giá trị mới cho cache
                        await _distributedCache.SetStringAsync(key, item, options);
                    }
                    else
                    {
                        cacheEntry = JsonSerializer.Deserialize<TItem>(itemString);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Có lỗi xảy ra khi xử lý dữ liệu từ redis - {ex.ToString()}");
                    cacheEntry = createItem();
                }

                return cacheEntry;
            }
            else
            {
                return createItem();
            }
        }

        public async Task Set<TItem>(string key, Func<Task<TItem>> createItem,
            DistributedCacheEntryOptions? options = null)
        {
            if (_config["AppSettings:EnableCache"] == "true")
            {
                try
                {
                    // Cache expire trong 300s
                    if (options == null)
                        options = new DistributedCacheEntryOptions().SetSlidingExpiration(
                            TimeSpan.FromSeconds(_timeLive));

                    var cacheEntry = await createItem();
                    var item = JsonSerializer.Serialize(cacheEntry);

                    // Nạp lại giá trị mới cho cache
                    await _distributedCache.SetStringAsync(key, item, options);
                }
                catch (Exception ex)
                {
                    Log.Error($"Có lỗi xảy ra khi xử lý dữ liệu từ redis - {ex.ToString()}");
                }
            }
        }

        public async Task Set<TItem>(string key, Func<TItem> createItem, DistributedCacheEntryOptions? options = null)
        {
            if (_config["AppSettings:EnableCache"] == "true")
            {
                try
                {
                    // Cache expire trong 300s
                    if (options == null)
                        options = new DistributedCacheEntryOptions().SetSlidingExpiration(
                            TimeSpan.FromSeconds(_timeLive));

                    var cacheEntry = createItem();
                    var item = JsonSerializer.Serialize(cacheEntry);

                    // Nạp lại giá trị mới cho cache
                    await _distributedCache.SetStringAsync(key, item, options);
                }
                catch (Exception ex)
                {
                    Log.Error($"Có lỗi xảy ra khi xử lý dữ liệu từ redis - {ex.ToString()}");
                }
            }
        }

        public async Task Set<TItem>(string key, TItem createItem, DistributedCacheEntryOptions? options = null)
        {
            if (_config["AppSettings:EnableCache"] == "true")
            {
                try
                {
                    // Cache expire trong 300s
                    if (options == null)
                        options = new DistributedCacheEntryOptions().SetSlidingExpiration(
                            TimeSpan.FromSeconds(_timeLive));

                    var item = JsonSerializer.Serialize(createItem);

                    // Nạp lại giá trị mới cho cache
                    await _distributedCache.SetStringAsync(key, item, options);
                }
                catch (Exception ex)
                {
                    Log.Error($"Có lỗi xảy ra khi xử lý dữ liệu từ redis - {ex.ToString()}");
                }
            }
        }

        public void Remove(string key)
        {
            if (_config["AppSettings:EnableCache"] == "true")
                _distributedCache.Remove(key);
        }

        public async void RemoveAll()
        {
            if (_config["AppSettings:EnableCache"] == "true" && !string.IsNullOrEmpty(_redisConnectionString))
            {
                var options = ConfigurationOptions.Parse(_redisConnectionString);
                options.AllowAdmin = true;
                var redis = ConnectionMultiplexer.Connect(options);

                var endpoints = redis.GetEndPoints();
                var server = redis.GetServer(endpoints.First());
                await server.FlushDatabaseAsync();

                var keys = server.Keys();
                foreach (var key in keys)
                {
                    _distributedCache.Remove(key.ToString());
                }
            }
        }

        public void RemoveAllWithPrefix(string prefix)
        {
            if (_config["AppSettings:EnableCache"] == "true" && !string.IsNullOrEmpty(_redisConnectionString))
            {
                var options = ConfigurationOptions.Parse(_redisConnectionString);
                options.AllowAdmin = true;
                var redis = ConnectionMultiplexer.Connect(options);

                var endpoints = redis.GetEndPoints();
                var server = redis.GetServer(endpoints.First());

                var keys = server.Keys();
                foreach (RedisKey key in keys)
                {
                    if (key.ToString().StartsWith(prefix))
                    {
                        _distributedCache.Remove(key.ToString());
                    }
                }
            }
        }
    }
}