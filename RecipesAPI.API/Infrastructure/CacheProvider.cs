using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace RecipesAPI.Infrastructure;

public interface ICacheProvider
{
    Task<T?> Get<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task Put<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task Remove(string key, CancellationToken cancellationToken = default);
    Task RemoveByPrefix(string keyPrefix, CancellationToken cancellationToken = default);
}

public class CacheProvider : ICacheProvider
{
    private readonly TimeSpan defaultExpiration = TimeSpan.FromHours(1);
    private readonly IDistributedCache _cache;
    private readonly string keyPrefix;
    private readonly IConnectionMultiplexer connectionMultiplexer;

    public CacheProvider(IDistributedCache cache, string keyPrefix, IConnectionMultiplexer connectionMultiplexer)
    {
        _cache = cache;
        this.keyPrefix = keyPrefix;
        this.connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<T?> Get<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var cachedUsers = await _cache.GetStringAsync(GetKey(key), cancellationToken);
        return cachedUsers == null ? null : JsonSerializer.Deserialize<T>(cachedUsers);
    }

    public async Task Put<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var users = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? defaultExpiration,
        };
        await _cache.SetStringAsync(GetKey(key), users, options, cancellationToken);
    }

    public async Task Remove(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(GetKey(key), cancellationToken);
    }

    public async Task RemoveByPrefix(string keyPrefix, CancellationToken cancellationToken = default)
    {
        await foreach (var key in GetKeysAsync(keyPrefix + "*"))
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
    }

    private async IAsyncEnumerable<string> GetKeysAsync(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(pattern));

        foreach (var endpoint in connectionMultiplexer.GetEndPoints())
        {
            var server = connectionMultiplexer.GetServer(endpoint);
            await foreach (var key in server.KeysAsync(pattern: GetKey(pattern)))
            {
                yield return key.ToString();
            }
        }
    }

    private string GetKey(string key)
    {
        return $"{keyPrefix}:{key}";
    }
}