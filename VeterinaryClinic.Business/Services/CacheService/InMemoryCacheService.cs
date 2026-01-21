using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog;
using VeterinaryClinic.Business;

namespace VeterinaryClinic.Business
{
 public class InMemoryCacheService : ICacheService
    {
        private readonly MemoryCache _cache;
        private ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();
        MemoryCacheEntryOptions cacheExpiryOptions;
        private readonly IConfiguration _config;

        public InMemoryCacheService(IConfiguration config)
        {
            _config = config;
            _cache = new MemoryCache(new MemoryCacheOptions());

            cacheExpiryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(60),
                Priority = CacheItemPriority.High,
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };
        }

        public async Task<TItem> GetOrCreate<TItem>(string key, Func<Task<TItem>> createItem, DistributedCacheEntryOptions options = null)
        {
            if (_config["AppSettings:EnableCache"] == "true")
            {
                TItem cacheEntry;

                if (!_cache.TryGetValue(key, out cacheEntry))// Look for cache key.
                {
                    SemaphoreSlim mylock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

                    await mylock.WaitAsync();
                    try
                    {
                        if (!_cache.TryGetValue(key, out cacheEntry))
                        {
                            // Key not in cache, so get data.
                            cacheEntry = await createItem();
                            _cache.Set(key, cacheEntry, cacheExpiryOptions);
                        }
                    }
                    finally
                    {
                        mylock.Release();
                    }
                }
                return cacheEntry;
            }
            else
            {
                return await createItem();
            }
        }

        public async Task<TItem> GetOrCreate<TItem>(string key, Func<TItem> createItem, DistributedCacheEntryOptions options = null)
        {
            if (_config["AppSettings:EnableCache"] == "true")
            {
                TItem cacheEntry;

                if (!_cache.TryGetValue(key, out cacheEntry))// Look for cache key.
                {
                    SemaphoreSlim mylock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

                    await mylock.WaitAsync();
                    try
                    {
                        if (!_cache.TryGetValue(key, out cacheEntry))
                        {
                            // Key not in cache, so get data.
                            cacheEntry = createItem();
                            _cache.Set(key, cacheEntry, cacheExpiryOptions);
                        }
                    }
                    finally
                    {
                        mylock.Release();
                    }
                }
                return cacheEntry;
            }
            else
            {
                return createItem();
            }
        }

        public async Task Set<TItem>(string key, Func<Task<TItem>> createItem, DistributedCacheEntryOptions options = null)
        {
            if (_config["AppSettings:EnableCache"] == "true")
            {
                TItem cacheEntry;
                try
                {
                    SemaphoreSlim mylock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

                    await mylock.WaitAsync();
                    try
                    {
                        // Key not in cache, so get data.
                        cacheEntry = await createItem();
                        _cache.Set(key, cacheEntry, cacheExpiryOptions);
                    }
                    finally
                    {
                        mylock.Release();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Có lỗi xảy ra khi xử lý dữ liệu từ redis - {ex.ToString()}");
                }
            }
        }

        public async Task Set<TItem>(string key, Func<TItem> createItem, DistributedCacheEntryOptions options = null)
        {
            if (_config["AppSettings:EnableCache"] == "true")
            {
                TItem cacheEntry;
                try
                {
                    SemaphoreSlim mylock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

                    await mylock.WaitAsync();
                    try
                    {
                        // Key not in cache, so get data.
                        cacheEntry = createItem();
                        _cache.Set(key, cacheEntry, cacheExpiryOptions);
                    }
                    finally
                    {
                        mylock.Release();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Có lỗi xảy ra khi xử lý dữ liệu từ redis - {ex.ToString()}");
                }
            }
        }
        public async Task Set<TItem>(string key, TItem createItem, DistributedCacheEntryOptions options = null)
        {
            if (_config["AppSettings:EnableCache"] == "true")
            {
                try
                {
                    SemaphoreSlim mylock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

                    await mylock.WaitAsync();
                    try
                    {
                        // Key not in cache, so get data.
                        _cache.Set(key, createItem, cacheExpiryOptions);
                    }
                    finally
                    {
                        mylock.Release();
                    }
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
                _cache.Remove(key);
        }

        public void RemoveAll()
        {
            throw new NotImplementedException();
        }

        public void RemoveAllWithPrefix(string prefix)
        {
            throw new NotImplementedException();
        }
    }   
}