using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Dapper;
using System.Data;
using MessagePack;

namespace RecipesAPI.API.Infrastructure;

public interface ICacheProvider
{
    Task<T?> Get<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task<List<T?>> Get<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken = default) where T : class;

    Task Put<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task Put<T>(IReadOnlyDictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    Task Remove(string key, CancellationToken cancellationToken = default);
    Task RemoveByPrefix(string keyPrefix, CancellationToken cancellationToken = default);
}

public class RedisCacheProvider : ICacheProvider
{
    private readonly TimeSpan defaultExpiration = TimeSpan.FromHours(1);
    private readonly IDistributedCache _cache;
    private readonly string keyPrefix;
    private readonly IConnectionMultiplexer connectionMultiplexer;

    public RedisCacheProvider(IDistributedCache cache, string keyPrefix, IConnectionMultiplexer connectionMultiplexer)
    {
        _cache = cache;
        this.keyPrefix = keyPrefix;
        this.connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<T?> Get<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var items = await _cache.GetStringAsync(GetKey(key), cancellationToken);
        return items == null ? null : JsonSerializer.Deserialize<T>(items);
    }

    public async Task<List<T?>> Get<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Use redis MGET command instead
        var result = new List<T?>();
        foreach (var key in keys)
        {
            var item = await Get<T>(key, cancellationToken);
            result.Add(item);
        }
        return result;
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

    public async Task Put<T>(IReadOnlyDictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        foreach (var kvp in keyValuePairs)
        {
            await Put(kvp.Key, kvp.Value, expiration, cancellationToken);
        }
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

/// <summary>
/// Sqlite cache provider
/// </summary>
/// <remarks>
/// TODO: Consider CBOR as alternative to messagepack
/// </remarks>
/// <param name="logger"></param>
/// <param name="sqliteDataContext"></param>
public class SqliteCacheProvider(ILogger<SqliteCacheProvider> logger, SqliteDataContext sqliteDataContext) : ICacheProvider
{
    private readonly TimeSpan defaultExpiration = TimeSpan.FromHours(1);

    private readonly ILogger<SqliteCacheProvider> logger = logger;
    private readonly SqliteDataContext sqliteDataContext = sqliteDataContext;

    public async Task<T?> Get<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            var val = await conn.QueryFirstOrDefaultAsync<byte[]>("SELECT Val FROM kv WHERE Key = @key", new { key });
            if (val == null) return null;
            var deserialized = MessagePackSerializer.Deserialize<T>(val, MessagePack.Resolvers.ContractlessStandardResolver.Options, cancellationToken);
            return deserialized;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get failed. key={key}", key);
            return null;
        }
    }

    public async Task<List<T?>> Get<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        if (keys == null || keys.Count == 0) return [];
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            var values = await conn.QueryAsync<(string key, byte[] val)>("SELECT Key, Val FROM kv WHERE key IN @keys", new { keys });
            var valuesByKey = values.GroupBy(x => x.key).ToDictionary(x => x.Key, x => x.First().val);
            var result = new List<T?>();
            foreach (var key in keys)
            {
                if (valuesByKey.TryGetValue(key, out var val))
                {
                    var deserialized = MessagePackSerializer.Deserialize<T>(val, MessagePack.Resolvers.ContractlessStandardResolver.Options, cancellationToken);
                    result.Add(deserialized);
                }
                else
                {
                    result.Add(null);
                }
            }
            return result;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get failed. keys={@keys}", keys);
            return [];
        }
    }

    public async Task Put<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            await InternalPut(conn, key, value, expiration, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Put failed. key={key}", key);
        }
    }

    public async Task Put<T>(IReadOnlyDictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (keyValuePairs == null || keyValuePairs.Count == 0) return;
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            foreach (var (key, val) in keyValuePairs)
            {
                await InternalPut(conn, key, val, expiration, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Put failed. keys={@keys}", keyValuePairs.Select(x => x.Key).ToList());
        }
    }

    public async Task InternalPut<T>(IDbConnection conn, string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var now = DateTime.UtcNow;
        await conn.ExecuteAsync(
            "INSERT OR REPLACE INTO kv(Key, Val, CreatedAt, ExpireAt) VALUES (@Key, @Val, @CreatedAt, @ExpireAt)",
             new
             {
                 Key = key,
                 Val = MessagePackSerializer.Serialize(value, MessagePack.Resolvers.ContractlessStandardResolver.Options, cancellationToken),
                 CreatedAt = now,
                 ExpireAt = now.Add(expiration ?? defaultExpiration)
             });
    }

    public async Task Remove(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            await conn.ExecuteAsync("DELETE FROM kv WHERE Key = @key", new { key });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Remove failed. key={key}", key);
        }
    }

    public async Task RemoveByPrefix(string keyPrefix, CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            await conn.ExecuteAsync("DELETE FROM kv WHERE Key LIKE @keyPrefix || '%'", new { keyPrefix });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RemoveByPrefix failed. keyPrefix={keyPrefix}", keyPrefix);
        }
    }

    public async Task RemoveExpired()
    {
        try
        {
            var now = DateTime.UtcNow;
            using var conn = sqliteDataContext.CreateCacheConnection();
            await conn.ExecuteAsync("DELETE FROM kv WHERE ExpireAt < @now", new { now });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RemoveExpired failed");
        }
    }
}