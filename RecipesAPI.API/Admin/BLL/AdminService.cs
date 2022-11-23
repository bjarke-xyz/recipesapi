using RecipesAPI.API.Admin.Common;
using RecipesAPI.API.Infrastructure;
using RecipesAPI.API.Recipes.BLL;

namespace RecipesAPI.API.Admin.BLL;

public class AdminService
{
    private readonly IEnumerable<ICacheKeyGetter> cacheKeyGetters;
    private readonly ICacheProvider cache;

    public AdminService(IEnumerable<ICacheKeyGetter> cacheKeyGetters, ICacheProvider cache)
    {
        this.cacheKeyGetters = cacheKeyGetters;
        this.cache = cache;
    }

    public List<CachedResourceType> GetCachedResourceTypes()
    {
        var cacheKeyInfos = cacheKeyGetters.Select(x => x.GetCacheKeyInfo().ResourceType).ToList();
        return cacheKeyInfos;
    }

    public async Task ClearCache(List<CachedResourceType> cachedResourceTypes, CancellationToken cancellationToken)
    {
        var cacheKeyInfos = cacheKeyGetters.Select(x => x.GetCacheKeyInfo()).ToDictionary(x => x.ResourceType.Name);
        foreach (var type in cachedResourceTypes)
        {
            if (cacheKeyInfos.TryGetValue(type.Name, out var cacheKeyInfo) && cacheKeyInfo != null)
            {
                foreach (var keyPrefix in cacheKeyInfo.CacheKeyPrefixes)
                {
                    await cache.RemoveByPrefix(keyPrefix, cancellationToken);
                }
            }
        }
    }
}