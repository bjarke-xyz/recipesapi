using System.Collections.Concurrent;
using Amazon.Auth.AccessControlPolicy;
using Lucene.Net.Analysis.Miscellaneous;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Admin.DAL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Admin.BLL;

public class AffiliateService(AdtractionService adtractionService, PartnerAdsService partnerAdsService, SettingsService settingsService, AffiliateSearchServiceV1 affiliateSearchServiceV1, AffiliateSearchServiceV2 affiliateSearchServiceV2, IConfiguration config)
{
    private readonly AdtractionService adtractionService = adtractionService;
    private readonly PartnerAdsService partnerAdsService = partnerAdsService;
    private readonly AffiliateSearchServiceV1 affiliateSearchServiceV1 = affiliateSearchServiceV1;
    private readonly AffiliateSearchServiceV2 affiliateSearchServiceV2 = affiliateSearchServiceV2;

    public async Task BuildSearchIndex(CancellationToken cancellationToken)
    {
        affiliateSearchServiceV2.ResetIndex();
        var adtractionPrograms = await adtractionService.GetPrograms("DK", null, config.GetValue<int>("AdtractionChannelId"), 1, null);
        const int limit = 1000;
        foreach (var adtractionProgram in adtractionPrograms)
        {
            foreach (var feed in adtractionProgram.Feeds ?? [])
            {
                int? afterId = null;
                int? productCount = null;
                while (productCount == null || productCount == limit)
                {
                    var adtractionFeedProducts = await adtractionService.GetFeedProducts(adtractionProgram.ProgramId, feed.FeedId, null, limit, null, true, afterId);
                    if (adtractionFeedProducts.Count == 0) break;
                    productCount = adtractionFeedProducts.Count;
                    afterId = adtractionFeedProducts.Last().ItemId;
                    var affiliateItems = adtractionFeedProducts.Select(x => new AffiliateItem(x)).ToList();
                    affiliateSearchServiceV2.IndexData(affiliateItems);
                }
            }
        }

        var partnerAdsPrograms = await partnerAdsService.GetPrograms(publicView: false);
        foreach (var partnerAdsProgram in partnerAdsPrograms)
        {
            int? afterId = null;
            int? productCount = null;
            while (productCount == null || productCount == limit)
            {
                var partnerAdsFeedProducts = await partnerAdsService.GetFeedProducts(partnerAdsProgram.ProgramId, partnerAdsProgram.FeedLink, null, limit, null, true, afterId);
                if (partnerAdsFeedProducts.Count == 0) break;
                productCount = partnerAdsFeedProducts.Count;
                afterId = partnerAdsFeedProducts.Last().ItemId;
                var affiliateItems = partnerAdsFeedProducts.Select(x => new AffiliateItem(x)).ToList();
                affiliateSearchServiceV2.IndexData(affiliateItems);
            }
        }
        affiliateSearchServiceV2.CommitIndex();
    }

    public async Task RefreshProductFeeds(CancellationToken cancellationToken)
    {
        await adtractionService.RefreshProductFeeds("DK", config.GetValue<int>("AdtractionChannelId"), null, null);
        await partnerAdsService.RefreshProductFeeds(null, null);
        await BuildSearchIndex(cancellationToken);
    }

    private async Task<AffiliateItem?> GetAdtractionItem(AffiliateItemReference itemReference)
    {
        if (itemReference.Adtraction == null) return null;
        var adtractionItem = await adtractionService.GetFeedProduct(itemReference.Adtraction);
        if (adtractionItem == null) return null;
        return new AffiliateItem(adtractionItem);
    }
    private async Task<List<AffiliateItem>> GetAdtractionItems(List<AffiliateItemReference> itemReferences)
    {
        var adtractionRefs = itemReferences.Where(x => x.Adtraction != null).Select(x => x.Adtraction!).ToList();
        var adtractionItems = await adtractionService.GetFeedProducts(adtractionRefs);
        return adtractionItems.Select(x => new AffiliateItem(x)).ToList();
    }

    private async Task<AffiliateItem?> GetPartnerAdsItem(AffiliateItemReference itemReference)
    {
        if (itemReference.PartnerAds == null) return null;
        var partnerAdsItem = await partnerAdsService.GetFeedProduct(itemReference.PartnerAds);
        if (partnerAdsItem == null) return null;
        return new AffiliateItem(partnerAdsItem);
    }
    private async Task<List<AffiliateItem>> GetPartnerAdsItems(List<AffiliateItemReference> itemReferences)
    {
        var partnerAdsRefs = itemReferences.Where(x => x.PartnerAds != null).Select(x => x.PartnerAds!).ToList();
        var partnerAdsItems = await partnerAdsService.GetFeedProducts(partnerAdsRefs);
        return partnerAdsItems.Select(x => new AffiliateItem(x)).ToList();
    }

    public async Task<AffiliateItem?> GetAffiliateItem(AffiliateItemReference itemReference) => itemReference.Provider switch
    {
        AffiliateProvider.Adtraction => await GetAdtractionItem(itemReference),
        AffiliateProvider.PartnerAds => await GetPartnerAdsItem(itemReference),
        _ => null,
    };

    public async Task<List<AffiliateItem>> GetAffiliateItems(List<AffiliateItemReference> itemReferences)
    {
        if (itemReferences.Count == 0) return [];
        using var activity = Telemetry.ActivitySource.StartActivity("GetAffiliateItems");
        activity?.AddTag("itemCount", itemReferences.Count);
        var byProvider = itemReferences.GroupBy(x => x.Provider).ToDictionary(x => x.Key, x => x.ToList());
        var items = new List<AffiliateItem>();
        foreach (var (provider, itemRefs) in byProvider)
        {
            switch (provider)
            {
                case AffiliateProvider.Adtraction:
                    var adtractionItems = await GetAdtractionItems(itemRefs);
                    items.AddRange(adtractionItems);
                    break;
                case AffiliateProvider.PartnerAds:
                    var partneradsItems = await GetPartnerAdsItems(itemRefs);
                    items.AddRange(partneradsItems);
                    break;
            }
        }
        return items;
    }

    public async Task<IReadOnlyDictionary<string, List<AffiliateItem>>> SearchAffiliateItems(List<string> searchQueries, int count, PartnerSettingsArea area)
    {
        var result = new ConcurrentDictionary<string, List<AffiliateItem>>();
        var settings = await settingsService.GetSettings();
        var partnerSettings = settings.PartnerSettings.Where(x => x.Area == area).ToList();
        await Parallel.ForEachAsync(searchQueries, async (searchQuery, cancellationToken) =>
        {
            var affiliateItems = await SearchAffiliateItems(searchQuery, count, partnerSettings);
            result[searchQuery] = affiliateItems;
        });
        return result;
    }

    public async Task<List<AffiliateItem>> SearchAffiliateItems(string? searchQuery, int count = 15, List<PartnerSettingsDto>? settings = null)
    {
        if (string.IsNullOrEmpty(searchQuery)) return [];
        using var activity = Telemetry.ActivitySource.StartActivity("SearchAffiliateItems");
        activity?.AddTag("searchQuery", searchQuery);
        activity?.AddTag("count", count);
        var version = 2;
        var searchResults = new List<AffiliateItemSearchDoc>();
        if (version == 1) searchResults = await affiliateSearchServiceV1.Search(searchQuery!);
        else if (version == 2) searchResults = affiliateSearchServiceV2.Search(searchQuery, count);
        var itemRefs = searchResults.Select(x => AffiliateItemReference.FromIdentifier(x.Id)).Where(x => x != null).Select(x => x!).ToList();
        var affiliateItems = await GetAffiliateItems(itemRefs);
        return affiliateItems;
    }

}