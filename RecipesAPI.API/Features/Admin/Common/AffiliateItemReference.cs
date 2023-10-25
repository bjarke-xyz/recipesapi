using System.ComponentModel;
using RecipesAPI.API.Features.Admin.Common.Adtraction;

namespace RecipesAPI.API.Features.Admin.Common;

public class AffiliateItem
{
    public AffiliateItem() { }
    public AffiliateItem(AdtractionFeedProduct adtractionFeedProduct)
    {
        Provider = AffiliateProvider.Adtraction;
        Adtraction = adtractionFeedProduct;
        if (!string.IsNullOrEmpty(adtractionFeedProduct.Brand) && !string.IsNullOrEmpty(adtractionFeedProduct.TrackingUrl))
        {
            ItemInfo = new AffiliateItemInfo
            {
                Title = adtractionFeedProduct.Brand,
                Url = adtractionFeedProduct.TrackingUrl,
                ImageUrl = adtractionFeedProduct.ImageUrl,
                Description = adtractionFeedProduct.Description,
                Category = adtractionFeedProduct.Category,
                NewPrice = adtractionFeedProduct.Price,
                OldPrice = adtractionFeedProduct.OriginalPrice,
                InStock = adtractionFeedProduct.InStock,
            };
        }
    }
    public AffiliateItem(PartnerAdsFeedProduct partnerAdsFeedProduct)
    {
        Provider = AffiliateProvider.PartnerAds;
        PartnerAds = partnerAdsFeedProduct;
        if (!string.IsNullOrEmpty(partnerAdsFeedProduct.Brand) && !string.IsNullOrEmpty(partnerAdsFeedProduct.ProductUrl))
        {
            ItemInfo = new AffiliateItemInfo
            {
                Title = partnerAdsFeedProduct.Brand,
                Url = partnerAdsFeedProduct.ProductUrl,
                ImageUrl = partnerAdsFeedProduct.ImageUrl,
                Description = partnerAdsFeedProduct.Description,
                Category = partnerAdsFeedProduct.CategoryName,
                NewPrice = partnerAdsFeedProduct.NewPrice,
                OldPrice = partnerAdsFeedProduct.OldPrice,
                InStock = partnerAdsFeedProduct.InStock,
            };
        }
    }
    public AffiliateProvider Provider { get; set; }
    public AdtractionFeedProduct? Adtraction { get; set; }
    public PartnerAdsFeedProduct? PartnerAds { get; set; }
    public AffiliateItemInfo? ItemInfo { get; set; } = new();
}

public class AffiliateItemInfo
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? NewPrice { get; set; }
    public decimal? OldPrice { get; set; }
    public bool? InStock { get; set; }
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