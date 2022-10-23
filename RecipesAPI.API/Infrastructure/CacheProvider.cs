using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace RecipesAPI.Infrastructure;

public interface ICacheProvider
{
    Task<T?> Get<T>(string key) where T : class;
    Task Put<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task Remove(string key);
}

public class CacheProvider : ICacheProvider
{
    private readonly TimeSpan defaultExpiration = TimeSpan.FromHours(1);
    private readonly IDistributedCache _cache;
    private readonly string keyPrefix;

    public CacheProvider(IDistributedCache cache, string keyPrefix)
    {
        _cache = cache;
        this.keyPrefix = keyPrefix;
    }

    public async Task<T?> Get<T>(string key) where T : class
    {
        var cachedUsers = await _cache.GetStringAsync(GetKey(key));
        return cachedUsers == null ? null : JsonSerializer.Deserialize<T>(cachedUsers);
    }

    public async Task Put<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var users = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? defaultExpiration,
        };
        await _cache.SetStringAsync(GetKey(key), users, options);
    }

    public async Task Remove(string key)
    {
        await _cache.RemoveAsync(GetKey(key));
    }

    private string GetKey(string key)
    {
        return $"{keyPrefix}:{key}";
    }
}