using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Dapper;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using RecipesAPI.API.Infrastructure.Serializers;
using System.Diagnostics.Metrics;
using Prometheus;

namespace RecipesAPI.API.Infrastructure;

public interface ICacheProvider
{
    Task<T?> Get<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task<Dictionary<string, T?>> Get<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken = default) where T : class;

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

    public async Task<Dictionary<string, T?>> Get<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Use redis MGET command instead
        var result = new Dictionary<string, T?>();
        foreach (var key in keys)
        {
            var item = await Get<T>(key, cancellationToken);
            result[key] = item;
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
/// <param name="logger"></param>
/// <param name="sqliteDataContext"></param>
/// <param name="serializer"></param>
public class SqliteCacheProvider(ILogger<SqliteCacheProvider> logger, SqliteDataContext sqliteDataContext, ISerializer serializer) : ICacheProvider
{
    private readonly TimeSpan defaultExpiration = TimeSpan.FromHours(1);

    private readonly ILogger<SqliteCacheProvider> logger = logger;
    private readonly SqliteDataContext sqliteDataContext = sqliteDataContext;

    private readonly ISerializer serializer = serializer;

    private static readonly Counter cacheRequests = Metrics
    .CreateCounter("recipesapi_cache_requests", "Number of cache requests.", labelNames: ["key"]);
    private static readonly Counter cacheMisses = Metrics
    .CreateCounter("recipesapi_cache_misses", "Number of cache misses.", labelNames: ["key"]);
    private static readonly Counter cacheHits = Metrics
    .CreateCounter("recipesapi_cache_hits", "Number of cache hits.", labelNames: ["key"]);



    private static Activity? StartActivity(string key, [CallerMemberName] string method = "")
    {
        var activity = Telemetry.ActivitySource.StartActivity($"SqliteCache:{method}");
        activity?.SetTag("key", key);
        return activity;
    }
    private static Activity? StartActivity(IEnumerable<string> keys, [CallerMemberName] string method = "")
    {
        var activity = Telemetry.ActivitySource.StartActivity($"SqliteCache:{method}");
        activity?.SetTag("key", string.Join(", ", keys));
        return activity;
    }
    private static void SetError(Activity? activity, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    }
    private string GetKey(string key)
    {
        return $"{serializer.GetTag()}{key}";
    }
    private List<string> GetKeys(IEnumerable<string> keys)
    {
        return keys.Select(GetKey).ToList();
    }

    public async Task<T?> Get<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        key = GetKey(key);
        cacheRequests.WithLabels(key).Inc();
        byte[]? val = null;
        using var activity = StartActivity(key);
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            val = await conn.QueryFirstOrDefaultAsync<byte[]>("SELECT Val FROM kv WHERE Key = @key", new { key });
            if (val == null)
            {
                cacheMisses.WithLabels(key).Inc();
                return null;
            }
            cacheHits.WithLabels(key).Inc();
            var deserialized = await serializer.Deserialize<T>(val, cancellationToken);
            return deserialized;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get failed. key={key}. val={val}", key, val);
            SetError(activity, ex);
            return null;
        }
    }

    public async Task<Dictionary<string, T?>> Get<T>(IReadOnlyList<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        if (keys == null || keys.Count == 0) return [];
        var modifiedKeys = GetKeys(keys);
        foreach (var k in modifiedKeys)
        {
            cacheRequests.WithLabels(k).Inc();
        }
        using var activity = StartActivity(modifiedKeys);
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            var values = await conn.QueryAsync<(string key, byte[] val)>("SELECT Key, Val FROM kv WHERE key IN @keys", new { keys = modifiedKeys });
            var valuesByKey = values.GroupBy(x => x.key).ToDictionary(x => x.Key, x => x.First().val);
            var result = new Dictionary<string, T?>();
            for (var i = 0; i < modifiedKeys.Count; i++)
            {
                var originalKey = keys[i];
                var key = modifiedKeys[i];
                if (valuesByKey.TryGetValue(key, out var val))
                {
                    cacheHits.WithLabels(key).Inc();
                    var deserialized = await serializer.Deserialize<T>(val, cancellationToken);
                    result[originalKey] = deserialized;
                }
                else
                {
                    cacheMisses.WithLabels(key).Inc();
                    result[originalKey] = null;
                }
            }
            return result;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get failed. keys={@keys}", modifiedKeys);
            SetError(activity, ex);
            return [];
        }
    }

    public async Task Put<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        key = GetKey(key);
        using var activity = StartActivity(key);
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            await InternalPut(conn, key, value, expiration, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Put failed. key={key}", key);
            SetError(activity, ex);
        }
    }

    public async Task Put<T>(IReadOnlyDictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (keyValuePairs == null || keyValuePairs.Count == 0) return;
        var keys = GetKeys(keyValuePairs.Select(x => x.Key));
        using var activity = StartActivity(keys);
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            foreach (var (_key, val) in keyValuePairs)
            {
                var key = GetKey(_key);
                await InternalPut(conn, key, val, expiration, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Put failed. keys={@keys}", keys);
            SetError(activity, ex);
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
                 Val = await serializer.Serialize(value, cancellationToken),
                 CreatedAt = now,
                 ExpireAt = now.Add(expiration ?? defaultExpiration)
             });
    }

    public async Task Remove(string key, CancellationToken cancellationToken = default)
    {
        key = GetKey(key);
        using var activity = StartActivity(key);
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            await conn.ExecuteAsync("DELETE FROM kv WHERE Key = @key", new { key });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Remove failed. key={key}", key);
            SetError(activity, ex);
        }
    }

    public async Task RemoveByPrefix(string keyPrefix, CancellationToken cancellationToken = default)
    {
        keyPrefix = GetKey(keyPrefix);
        using var activity = StartActivity(keyPrefix);
        try
        {
            using var conn = sqliteDataContext.CreateCacheConnection();
            await conn.ExecuteAsync("DELETE FROM kv WHERE Key LIKE @keyPrefix || '%'", new { keyPrefix });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RemoveByPrefix failed. keyPrefix={keyPrefix}", keyPrefix);
            SetError(activity, ex);
        }
    }

    public async Task RemoveExpired()
    {
        using var activity = StartActivity("");
        try
        {
            var now = DateTime.UtcNow;
            using var conn = sqliteDataContext.CreateCacheConnection();
            await conn.ExecuteAsync("DELETE FROM kv WHERE ExpireAt < @now", new { now });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RemoveExpired failed");
            SetError(activity, ex);
        }
    }
}
