using RecipesAPI.Admin.Common;

namespace RecipesAPI.Infrastructure;

public interface ICacheKeyGetter
{
    CacheKeyInfo GetCacheKeyInfo();
}

public class CacheKeyInfo
{
    public IReadOnlyList<string> CacheKeyPrefixes { get; set; } = default!;
    public CachedResourceType ResourceType { get; set; } = default!;
}