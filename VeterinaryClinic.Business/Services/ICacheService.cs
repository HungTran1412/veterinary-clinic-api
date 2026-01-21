using Microsoft.Extensions.Caching.Distributed;

namespace VeterinaryClinic.Business;

public interface ICacheService
{
    Task<TItem?> GetOrCreate<TItem>(string key, Func<Task<TItem>> createItem, DistributedCacheEntryOptions? options = null);
    Task<TItem?> GetOrCreate<TItem>(string key, Func<TItem> createItem, DistributedCacheEntryOptions? options = null);
    Task Set<TItem>(string key, Func<Task<TItem>> createItem, DistributedCacheEntryOptions? options = null);
    Task Set<TItem>(string key, Func<TItem> createItem, DistributedCacheEntryOptions? options = null);
    Task Set<TItem>(string key, TItem createItem, DistributedCacheEntryOptions? options = null);
    void Remove(string key);
    void RemoveAll();
    void RemoveAllWithPrefix(string prefix);
}