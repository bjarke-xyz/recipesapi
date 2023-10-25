using RecipesAPI.API.Features.Admin.Common.Adtraction;

namespace RecipesAPI.API.Features.Admin.Common;

public class AffiliateItem
{
    public AffiliateItem() { }
    public AffiliateItem(AdtractionFeedProduct adtractionFeedProduct)
    {
        Provider = AffiliateProvider.Adtraction;
        Adtraction = adtractionFeedProduct;
    }
    public AffiliateItem(PartnerAdsFeedProduct partnerAdsFeedProduct)
    {
        Provider = AffiliateProvider.PartnerAds;
        PartnerAds = partnerAdsFeedProduct;
    }
    public AffiliateProvider Provider { get; set; }
    public AdtractionFeedProduct? Adtraction { get; set; }
    public PartnerAdsFeedProduct? PartnerAds { get; set; }
}

public class AffiliateItemReference
{
    public AffiliateProvider Provider { get; set; }
    public AdtractionItemReference? Adtraction { get; set; }
    public PartnerAdsItemReference? PartnerAds { get; set; }
}

public class AdtractionItemReference
{
    public int ProgramId { get; set; }
    public int FeedId { get; set; }
    public string Sku { get; set; } = "";
}

public class PartnerAdsItemReference
{
    public string ProgramId { get; set; } = "";
    public string ProductId { get; set; } = "";
}

public enum AffiliateProvider
{
    NoValue = 0,
    Adtraction = 1,
    PartnerAds = 2,
}