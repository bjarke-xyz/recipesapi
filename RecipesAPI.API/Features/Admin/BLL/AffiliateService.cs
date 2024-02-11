using System.Collections.Concurrent;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Admin.DAL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Admin.BLL;

public class AffiliateService(AdtractionService adtractionService, PartnerAdsService partnerAdsService, SettingsService settingsService, AffiliateSearchServiceV1 affiliateSearchServiceV1)
{
    private readonly AdtractionService adtractionService = adtractionService;
    private readonly PartnerAdsService partnerAdsService = partnerAdsService;
    private readonly AffiliateSearchServiceV1 affiliateSearchServiceV1 = affiliateSearchServiceV1;

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

    public async Task<List<AffiliateItem>> SearchAffiliateItems(string? searchQuery, int count = 100, List<PartnerSettingsDto>? settings = null)
    {
        if (string.IsNullOrEmpty(searchQuery)) return [];
        var version = 1;
        var searchResults = new List<AffiliateItemSearchDoc>();
        if (version == 1)
        {
            searchResults = await affiliateSearchServiceV1.Search(searchQuery!);
        }
        var itemRefs = searchResults.Select(x => AffiliateItemReference.FromIdentifier(x.Id)).Where(x => x != null).Select(x => x!).ToList();
        var affiliateItems = await GetAffiliateItems(itemRefs);
        return affiliateItems;
    }

}