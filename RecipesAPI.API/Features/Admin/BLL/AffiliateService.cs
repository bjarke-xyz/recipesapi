using RecipesAPI.API.Features.Admin.Common;
using SQLitePCL;

namespace RecipesAPI.API.Features.Admin.BLL;

public class AffiliateService(AdtractionService adtractionService, PartnerAdsService partnerAdsService)
{
    private readonly AdtractionService adtractionService = adtractionService;
    private readonly PartnerAdsService partnerAdsService = partnerAdsService;

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

    public async Task<List<AffiliateItem>> SearchAffiliateItems(AffiliateProvider provider, string? searchQuery, string? programId, int skip, int limit) => provider switch
    {
        AffiliateProvider.Adtraction => await SearchAdtractionItems(searchQuery, programId, skip, limit),
        AffiliateProvider.PartnerAds => await SearchPartnerAdsItems(searchQuery, programId, skip, limit),
        _ => [],
    };

}