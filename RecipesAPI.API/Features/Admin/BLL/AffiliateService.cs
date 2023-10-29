using HotChocolate.Language;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Infrastructure;
using SQLitePCL;

namespace RecipesAPI.API.Features.Admin.BLL;

public class AffiliateService(AdtractionService adtractionService, PartnerAdsService partnerAdsService, ICacheProvider cache)
{
    private readonly AdtractionService adtractionService = adtractionService;
    private readonly PartnerAdsService partnerAdsService = partnerAdsService;
    private readonly ICacheProvider cache = cache;

    private async Task<AffiliateItem?> GetAdtractionItem(AffiliateItemReference itemReference)
    {
        if (itemReference.Adtraction == null) return null;
        var adtractionItem = await adtractionService.GetFeedProduct(itemReference.Adtraction);
        if (adtractionItem == null) return null;
        return new AffiliateItem(adtractionItem);
    }
    private async Task<List<AffiliateItem>> SearchAdtractionItems(string? searchQuery, string? programId, int skip, int limit)
    {
        var adtractionFeedProducts = await adtractionService.SearchFeedProducts(searchQuery, programId, skip, limit);
        var adtractionAffiliateItems = adtractionFeedProducts.Select(x => new AffiliateItem(x)).ToList();
        return adtractionAffiliateItems;

    }

    private async Task<AffiliateItem?> GetPartnerAdsItem(AffiliateItemReference itemReference)
    {
        if (itemReference.PartnerAds == null) return null;
        var partnerAdsItem = await partnerAdsService.GetFeedProduct(itemReference.PartnerAds);
        if (partnerAdsItem == null) return null;
        return new AffiliateItem(partnerAdsItem);
    }

    private async Task<List<AffiliateItem>> SearchPartnerAdsItems(string? searchQuery, string? programId, int skip, int limit)
    {
        var partnerAdsFeedProducts = await partnerAdsService.SearchFeedProducts(searchQuery, programId, skip, limit);
        var affiliateItems = partnerAdsFeedProducts.Select(x => new AffiliateItem(x)).ToList();
        return affiliateItems;
    }

    public async Task<AffiliateItem?> GetAffiliateItem(AffiliateItemReference itemReference) => itemReference.Provider switch
    {
        AffiliateProvider.Adtraction => await GetAdtractionItem(itemReference),
        AffiliateProvider.PartnerAds => await GetPartnerAdsItem(itemReference),
        _ => null,
    };

    public async Task<Dictionary<string, List<AffiliateItem>>> SearchAffiliateItems(List<string> searchQueries, int count)
    {
        var result = new Dictionary<string, List<AffiliateItem>>();
        foreach (var searchQuery in searchQueries)
        {
            var affiliateItems = await SearchAffiliateItems(searchQuery, count);
            result[searchQuery] = affiliateItems;
        }
        return result;
    }

    public async Task<List<AffiliateItem>> SearchAffiliateItems(string? searchQuery, int count = 100)
    {
        const int cacheCount = 15;
        var originalCount = count;
        if (count <= cacheCount)
        {
            count = cacheCount;
        }
        var cacheKey = $"AffSvc:SearchItems:{searchQuery}:{count}";
        var cached = count == cacheCount ? await cache.Get<List<AffiliateItem>>(cacheKey) : null;
        if (cached != null)
        {
            return cached.Take(originalCount).ToList();
        }
        var providers = new List<AffiliateProvider> { AffiliateProvider.Adtraction, AffiliateProvider.PartnerAds };
        var allItems = new List<AffiliateItem>();
        foreach (var provider in providers)
        {
            var providerItems = await InternalSearchAffiliateItems(provider, searchQuery, null, 0, 500);
            allItems.AddRange(providerItems);
        }

        var rankedItems = RankItems(allItems, searchQuery, count);
        if (count == cacheCount)
        {
            await cache.Put(cacheKey, rankedItems, expiration: TimeSpan.FromHours(1));
        }
        return rankedItems.Take(originalCount).ToList();
    }

    private List<AffiliateItem> RankItems(List<AffiliateItem> allItems, string? searchQuery, int count)
    {
        if (count > 1000) count = 1000;
        if (string.IsNullOrEmpty(searchQuery)) return allItems.Take(count).ToList();
        var rankedItems = new List<(long score, AffiliateItem item)>();

        foreach (var item in allItems)
        {
            var score = CalculateScore(item, searchQuery);
            rankedItems.Add((score, item));
        }
        var orderedRankedItems = rankedItems.OrderBy(x => x.score).Take(count).ToList();
        return orderedRankedItems.Select(x => x.item).ToList();
    }

    /// <summary>
    /// Calculate score for matching searchQuery. Lower is better
    /// </summary>
    /// <param name="item"></param>
    /// <param name="searchQuery"></param>
    /// <returns></returns>
    private int CalculateScore(AffiliateItem item, string searchQuery)
    {
        if (item.ItemInfo == null) return int.MaxValue;

        // Full name equals search query
        if (string.Equals(item.ItemInfo.ProductName, searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        // Full category name equals search query
        if (string.Equals(item.ItemInfo.Category, searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        // Mega sej kniv -> matches searchquery 'kniv'
        var nameParts = item.ItemInfo.ProductName.Split(" ");
        if (nameParts.Contains(searchQuery, StringComparer.OrdinalIgnoreCase))
        {
            return 1000;
        }

        // Full word of category matches searchquery
        var categoryParts = item.ItemInfo.Category?.Split(" ") ?? [];
        if (categoryParts.Contains(searchQuery, StringComparer.OrdinalIgnoreCase))
        {
            return 1001;
        }

        // Mega sej køkkenkniv -> matches endwith first
        // Mega sej knivsæt -> matches startswith after
        foreach (var namePart in nameParts)
        {
            if (namePart.EndsWith(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 2000;
            }
            if (namePart.StartsWith(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 2100;
            }
        }

        foreach (var categoryPart in categoryParts)
        {
            if (categoryPart.EndsWith(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 2001;
            }
            if (categoryPart.StartsWith(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 2101;
            }
        }

        // "Ellen Knivsbærer 1 - Ungdomsbog - hardback" -> matches because it contains 'kniv'
        if (item.ItemInfo.ProductName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 3000;
        }

        if (item.ItemInfo.Category?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true)
        {
            return 3001;
        }

        return int.MaxValue - 1; // minus one, to weight higher than null itemInfo
    }

    private async Task<List<AffiliateItem>> InternalSearchAffiliateItems(AffiliateProvider provider, string? searchQuery, string? programId, int skip, int limit) => provider switch
    {
        AffiliateProvider.Adtraction => await SearchAdtractionItems(searchQuery, programId, skip, limit),
        AffiliateProvider.PartnerAds => await SearchPartnerAdsItems(searchQuery, programId, skip, limit),
        _ => [],
    };

}