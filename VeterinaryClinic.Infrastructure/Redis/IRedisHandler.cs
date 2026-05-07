namespace VeterinaryClinic.Business.Core;

public interface IRedisHandler
{
    Task<bool> SetLongValueAsync(string key, long value);
    Task<long> StringDecrementAsync(string key);
    Task<long> StringIncrementAsync(string key);
        
    Task<long> StringIncrementAsync(Func<Task<long>> func, string key, int valueIncrease);
        
    Task<bool> DeleteAsync(string key);
    Task<bool> DeleteHashAsync(string key, string hashKey);

    Task<bool> SetAsync(string key, string value);
    Task<bool> SetAsync(string key, string value, TimeSpan expire);
    Task<string> GetAsync(string key);

    Task<bool> SetObjectAsync<T>(string key, T value);
    Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan expire);
    Task<T> GetObjectAsync<T>(string key);

    Task<bool> SetListAsync<T>(string key, List<T> value);
    Task<bool> SetListAsync<T>(string key, List<T> value, TimeSpan expire);
    Task<List<T>> GetListAsync<T>(string key);

    Task<bool> SetHashAsync<T>(string key, string hashKey, T value);
    Task<bool> SetHashAsync<T>(string key, string hashKey, T value, TimeSpan expire);
    Task<T> GetHashAsync<T>(string key, string hashKey);

    Task SetListHashAsync<T>(string key, List<T> value);
    Task SetListHashAsync<T>(string key, List<T> value, TimeSpan expire);
    Task<List<T>> GetListHashAsync<T>(string key);

    //Task<bool> SetSetAsync<T>(string key, List<T> value);
    //Task<bool> SetSetAsync<T>(string key, List<T> value, TimeSpan expire);
    //Task<List<T>> GetSetAsync<T>(string key);

    Task<long> SetSortedSetAsync<T>(string key, List<T> value);
    Task<long> SetSortedSetAsync<T>(string key, List<T> value, TimeSpan expire);
    Task<List<T>> GetSortedSetAsync<T>(string key);
}