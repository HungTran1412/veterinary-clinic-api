using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Serilog;
using StackExchange.Redis;
using VeterinaryClinic.Business.Core;

namespace VeterinaryClinic.Infrastructure
{
    // https://github.com/redis/NRedisStack

    public class RedisHandler : IRedisHandler
    {
        private IDatabase _db;
        private readonly IConfiguration _config;

        public RedisHandler(IConfiguration config)
        {
            _config = config;
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(_config.GetConnectionString("Redis"));
            _db = redis.GetDatabase();
        }

        #region Increase - Decrease
        public async Task<bool> SetLongValueAsync(string key, long value)
        {
            var dt = await _db.StringSetAsync(key, value);
            return dt;
        }

        public async Task<long> StringDecrementAsync(string key)
        {
            var dt = await _db.StringDecrementAsync(key);
            return dt;
        }

        public async Task<long> StringIncrementAsync(string key)
        {
            var dt = await _db.StringIncrementAsync(key);
            return dt;
        }

        public async Task<long> StringIncrementAsync(Func<Task<long>> func, string key, int valueIncrease)
        {
            // Build key
            var redisKey = $"IncrementData:{key}";

            // Kiểm tra xem key đã tồn tại trong Redis chưa
            var existingValue = await _db.StringGetAsync(redisKey);

            // Nếu chưa có trong Redis, khởi tạo từ DB
            if (string.IsNullOrEmpty(existingValue))
            {
                var initialValue = await func();

                // Set giá trị khởi tạo vào Redis
                await _db.StringSetAsync(redisKey, initialValue);
            }
            Log.Information($"StringIncrementAsync: {redisKey} - {existingValue} - {valueIncrease}");
            return await _db.StringIncrementAsync(redisKey, valueIncrease);
        }

        #endregion

        #region Delete by key
        public async Task<bool> DeleteAsync(string key)
        {
            return await _db.KeyDeleteAsync(key);
        }

        public async Task<bool> DeleteHashAsync(string key, string hashKey)
        {
            return await _db.HashDeleteAsync(key, hashKey);
        }

        #endregion

        #region String
        public async Task<bool> SetAsync(string key, string value)
        {
            return await _db.StringSetAsync(key, value);
        }

        public async Task<bool> SetAsync(string key, string value, TimeSpan expire)
        {
            return await _db.StringSetAsync(key, value, expire);
        }

        public async Task<string> GetAsync(string key)
        {
            return await _db.StringGetAsync(key);
        }
        #endregion

        #region Object as string
        public async Task<bool> SetObjectAsync<T>(string key, T value)
        {
            return await _db.StringSetAsync(key, JsonSerializer.Serialize(value));
        }

        public async Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan expire)
        {
            return await _db.StringSetAsync(key, JsonSerializer.Serialize(value), expire);
        }

        public async Task<T> GetObjectAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);
            if (value.HasValue)
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            return default(T);
        }
        #endregion

        #region List as string
        public async Task<bool> SetListAsync<T>(string key, List<T> value)
        {
            return await _db.StringSetAsync(key, JsonSerializer.Serialize(value));
        }

        public async Task<bool> SetListAsync<T>(string key, List<T> value, TimeSpan expire)
        {
            return await _db.StringSetAsync(key, JsonSerializer.Serialize(value), expire);
        }

        public async Task<List<T>> GetListAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);
            if (value.HasValue)
            {
                return JsonSerializer.Deserialize<List<T>>(value);
            }
            return default(List<T>);
        }
        #endregion

        #region Hash
        public async Task<bool> SetHashAsync<T>(string key, string hashKey, T value)
        {
            return await _db.HashSetAsync(key, hashKey, JsonSerializer.Serialize(value));
        }

        public async Task<bool> SetHashAsync<T>(string key, string hashKey, T value, TimeSpan expire)
        {
            return await _db.HashSetAsync(key, hashKey, JsonSerializer.Serialize(value)) && await _db.KeyExpireAsync(key, expire);
        }

        public async Task<T> GetHashAsync<T>(string key, string hashKey)
        {
            var value = await _db.HashGetAsync(key, hashKey);
            if (value.HasValue)
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            return default(T);
        }
        #endregion

        #region List as Hash
        public async Task SetListHashAsync<T>(string key, List<T> value)
        {
            var hashEntries = new HashEntry[value.Count];
            for (int i = 0; i < value.Count; i++)
            {
                hashEntries[i] = new HashEntry(i.ToString(), JsonSerializer.Serialize(value[i]));
            }
            await _db.HashSetAsync(key, hashEntries);
        }

        public async Task SetListHashAsync<T>(string key, List<T> value, TimeSpan expire)
        {
            var hashEntries = new HashEntry[value.Count];
            for (int i = 0; i < value.Count; i++)
            {
                hashEntries[i] = new HashEntry(i.ToString(), JsonSerializer.Serialize(value[i]));
            }
            await _db.HashSetAsync(key, hashEntries);
            await _db.KeyExpireAsync(key, expire);
        }

        public async Task<List<T>> GetListHashAsync<T>(string key)
        {
            var value = await _db.HashGetAllAsync(key);
            if (value.Length > 0)
            {
                var result = new List<T>();
                foreach (var item in value)
                {
                    result.Add(JsonSerializer.Deserialize<T>(item.Value));
                }
                return result;
            }
            return default(List<T>);
        }
        #endregion

        #region SortedSet
        public async Task<long> SetSortedSetAsync<T>(string key, List<T> value)
        {
            var hashEntries = new SortedSetEntry[value.Count];
            for (int i = 0; i < value.Count; i++)
            {
                hashEntries[i] = new SortedSetEntry(JsonSerializer.Serialize(value[i]), i);
            }
            return await _db.SortedSetAddAsync(key, hashEntries);
        }

        public async Task<long> SetSortedSetAsync<T>(string key, List<T> value, TimeSpan expire)
        {
            var hashEntries = new SortedSetEntry[value.Count];
            for (int i = 0; i < value.Count; i++)
            {
                hashEntries[i] = new SortedSetEntry(JsonSerializer.Serialize(value[i]), i);
            }
            var dt = await _db.SortedSetAddAsync(key, hashEntries);
            await _db.KeyExpireAsync(key, expire);
            return dt;
        }

        public async Task<List<T>> GetSortedSetAsync<T>(string key)
        {
            var value = await _db.SortedSetRangeByRankAsync(key);
            if (value.Length > 0)
            {
                var result = new List<T>();
                foreach (var item in value)
                {
                    result.Add(JsonSerializer.Deserialize<T>(item));
                }
                return result;
            }
            return default(List<T>);
        }
        #endregion
    }
}
