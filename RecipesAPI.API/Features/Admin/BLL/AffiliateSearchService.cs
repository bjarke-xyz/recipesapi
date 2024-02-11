using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Food.BLL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Admin.BLL;

public class AffiliateSearchServiceV2
{
    // TODO: search using lucene
}

public class AffiliateSearchServiceV1(ICacheProvider cache, PartnerAdsService partnerAdsService, AdtractionService adtractionService)
{
    private readonly ICacheProvider cache = cache;
    private readonly PartnerAdsService partnerAdsService = partnerAdsService;
    private readonly AdtractionService adtractionService = adtractionService;

    public async Task<List<AffiliateItemSearchDoc>> Search(string searchQuery)
    {
        var positiveTags = new List<string>();
        var negativeTags = new List<string>();
        var categories = new List<string>();
        var cachingEnabled = false;
        var count = 15;
        const int cacheCount = 15;
        var originalCount = count;
        if (count <= cacheCount)
        {
            count = cacheCount;
        }
        var cacheKey = $"AffSvc:SearchItemDocs:{searchQuery}:{count}";
        var cached = cachingEnabled && count == cacheCount ? await cache.Get<List<AffiliateItemSearchDoc>>(cacheKey) : null;
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

        var rankedItems = RankItems(allItems, searchQuery, count, negativeTags, positiveTags, categories);
        if (count == cacheCount)
        {
            await cache.Put(cacheKey, rankedItems, expiration: TimeSpan.FromHours(1));
        }
        var rankedItemsSubset = rankedItems.Take(originalCount).ToList();
        var searchResult = rankedItemsSubset.Select((item, i) => new AffiliateItemSearchDoc(item, rankedItemsSubset.Count - i)).ToList();
        return searchResult;
    }

    private List<AffiliateItem> RankItems(List<AffiliateItem> allItems, string? searchQuery, int count, List<string> negativeTags, List<string> positiveTags, List<string> categories)
    {
        if (count > 1000) count = 1000;
        if (string.IsNullOrEmpty(searchQuery)) return allItems.Take(count).ToList();
        var rankedItems = new List<(long score, AffiliateItem item)>();

        foreach (var item in allItems)
        {
            var score = CalculateInitialScore(item, searchQuery);
            score = AdjustScore(score, item, negativeTags, positiveTags, categories);
            rankedItems.Add((score, item));
        }

        var orderedRankedItems = rankedItems.OrderBy(x => x.score).Take(count).ToList();

        AdjustScore(orderedRankedItems);

        orderedRankedItems = orderedRankedItems.OrderBy(x => x.score).ToList();
        return orderedRankedItems.Select(x => x.item).ToList();
    }

    private static void AdjustScore(List<(long, AffiliateItem)> rankedItems)
    {
        var seenBrandsCount = new Dictionary<string, int>();
        for (var i = 0; i < rankedItems.Count; i++)
        {
            var (score, item) = rankedItems[i];
            if (item.ItemInfo == null) continue;
            var newScore = score;
            if (!string.IsNullOrEmpty(item.ItemInfo.Brand))
            {
                if (seenBrandsCount.TryGetValue(item.ItemInfo.Brand, out var seenCount) && seenCount >= 2)
                {
                    newScore += 500 * seenCount; // TODO: possibly not a good value
                }
                seenBrandsCount[item.ItemInfo.Brand] = seenBrandsCount.GetValueOrDefault(item.ItemInfo.Brand, 0) + 1;
            }
            rankedItems[i] = (newScore, item);
        }

    }

    private static int AdjustScore(int score, AffiliateItem item, List<string> negativeTags, List<string> positiveTags, List<string> categories)
    {
        var newScore = score;
        if (item.ItemInfo == null) return newScore;
        var hasDiscount = item.ItemInfo.NewPrice < item.ItemInfo.OldPrice;
        if (hasDiscount)
        {
            newScore -= 100;
        }
        if (item.ItemInfo.InStock == true)
        {
            newScore -= 100;
        }
        foreach (var category in categories)
        {
            if (item.ItemInfo.Category?.Contains(category, StringComparison.OrdinalIgnoreCase) == true)
            {
                newScore -= 1000;
            }
        }
        foreach (var positiveTag in positiveTags)
        {
            if (item.ItemInfo.Title.Contains(positiveTag, StringComparison.OrdinalIgnoreCase)
            || item.ItemInfo.Category?.Contains(positiveTag, StringComparison.OrdinalIgnoreCase) == true
            )
            {
                newScore -= 1000;
            }
        }
        foreach (var negativeTag in negativeTags)
        {
            if (item.ItemInfo.Title.Contains(negativeTag, StringComparison.OrdinalIgnoreCase)
            || item.ItemInfo.Category?.Contains(negativeTag, StringComparison.OrdinalIgnoreCase) == true
            )
            {
                newScore += 1000;
            }
        }
        if (string.IsNullOrWhiteSpace(item.ItemInfo.Brand))
        {
            newScore += 1000;
        }
        if (string.IsNullOrWhiteSpace(item.ItemInfo.Category))
        {
            newScore += 1000;
        }
        if (string.IsNullOrWhiteSpace(item.ItemInfo.Description))
        {
            newScore += 1000;
        }
        return newScore;
    }

    /// <summary>
    /// Calculate score for matching searchQuery. Lower is better
    /// </summary>
    /// <param name="item"></param>
    /// <param name="searchQuery"></param>
    /// <returns></returns>
    private int CalculateInitialScore(AffiliateItem item, string searchQuery)
    {
        if (item.ItemInfo == null) return int.MaxValue;

        // Full name equals search query
        if (string.Equals(item.ItemInfo.ProductName, searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var pluralSearchQueries = Plural(searchQuery);

        // Full category name equals search query
        if (string.Equals(item.ItemInfo.Category, searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }
        foreach (var pluralSearchQuery in pluralSearchQueries)
        {
            if (string.Equals(item.ItemInfo.Category, pluralSearchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

        }

        // Mega sej kniv -> matches searchquery 'kniv'
        // earlier match -> lower score
        var nameParts = item.ItemInfo.ProductName.Split(" ");
        for (var i = 0; i < nameParts.Length; i++)
        {
            var namePart = nameParts[i];
            if (string.Equals(namePart, searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                // return 1000 + ((nameParts.Length - i - 1) * 100);
                return 1000 + (i * 100);
            }
        }

        // Full word of category matches searchquery
        var categoryParts = item.ItemInfo.Category?.Split(" ") ?? [];
        for (var i = 0; i < categoryParts.Length; i++)
        {
            var categoryPart = categoryParts[i];
            if (string.Equals(categoryPart, searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                // return 1001 + ((categoryParts.Length - i - 1) * 100);
                return 1001 + (i * 100);
            }
            foreach (var pluralSearchQuery in pluralSearchQueries)
            {
                if (string.Equals(categoryPart, pluralSearchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    // return 1001 + ((categoryParts.Length - i - 1) * 100);
                    return 1002 + (i * 100);
                }
            }
        }

        // Mega sej køkkenkniv -> matches endwith first
        // Mega sej knivsæt -> matches startswith after
        foreach (var namePart in nameParts)
        {
            if (namePart.EndsWith(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 1100;
            }
            if (namePart.StartsWith(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 1200;
            }

            foreach (var pluralSearchQuery in pluralSearchQueries)
            {
                if (namePart.EndsWith(pluralSearchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return 1101;
                }
                if (namePart.StartsWith(pluralSearchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return 1201;
                }
            }
        }

        foreach (var categoryPart in categoryParts)
        {
            if (categoryPart.EndsWith(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 2502;
            }
            if (categoryPart.StartsWith(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 2602;
            }
            foreach (var pluralSearchQuery in pluralSearchQueries)
            {
                if (categoryPart.EndsWith(pluralSearchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return 2503;
                }
                if (categoryPart.StartsWith(pluralSearchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return 2603;
                }
            }
        }

        // "Ellen Knivsbærer 1 - Ungdomsbog - hardback" -> matches because it contains 'kniv'
        if (item.ItemInfo.ProductName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 3000;
        }
        foreach (var pluralSearchQuery in pluralSearchQueries)
        {
            if (item.ItemInfo.ProductName.Contains(pluralSearchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 3001;
            }
        }

        if (item.ItemInfo.Category?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true)
        {
            return 3002;
        }
        foreach (var pluralSearchQuery in pluralSearchQueries)
        {
            if (item.ItemInfo.Category?.Contains(pluralSearchQuery, StringComparison.OrdinalIgnoreCase) == true)
            {
                return 3003;
            }

        }

        return int.MaxValue - 1; // minus one, to weight higher than null itemInfo
    }

    private static IReadOnlyList<string> Plural(string searchQuery)
    {
        return [searchQuery + "er", searchQuery + "e"];
    }

    private async Task<List<AffiliateItem>> InternalSearchAffiliateItems(AffiliateProvider provider, string? searchQuery, string? programId, int skip, int limit) => provider switch
    {
        AffiliateProvider.Adtraction => await SearchAdtractionItems(searchQuery, programId, skip, limit),
        AffiliateProvider.PartnerAds => await SearchPartnerAdsItems(searchQuery, programId, skip, limit),
        _ => [],
    };
    private async Task<List<AffiliateItem>> SearchAdtractionItems(string? searchQuery, string? programId, int skip, int limit)
    {
        var adtractionFeedProducts = await adtractionService.SearchFeedProducts(searchQuery, programId, skip, limit);
        var adtractionAffiliateItems = adtractionFeedProducts.Select(x => new AffiliateItem(x)).ToList();
        return adtractionAffiliateItems;

    }
    private async Task<List<AffiliateItem>> SearchPartnerAdsItems(string? searchQuery, string? programId, int skip, int limit)
    {
        var partnerAdsFeedProducts = await partnerAdsService.SearchFeedProducts(searchQuery, programId, skip, limit);
        var affiliateItems = partnerAdsFeedProducts.Select(x => new AffiliateItem(x)).ToList();
        return affiliateItems;
    }
}

public record AffiliateItemSearchDoc(string Id, string Title, string? Description, string? Category, string? Brand, string ProductName, float Score)
{
    public AffiliateItemSearchDoc(AffiliateItem item, float score) : this(item.ItemReference.ToIdentifier(), item.ItemInfo!.Title, item.ItemInfo.Description, item.ItemInfo.Category, item.ItemInfo.Brand, item.ItemInfo.ProductName, score) { }
}