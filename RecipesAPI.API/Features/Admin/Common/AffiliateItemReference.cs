using System.ComponentModel;
using RecipesAPI.API.Features.Admin.Common.Adtraction;

namespace RecipesAPI.API.Features.Admin.Common;

public class AffiliateItem
{
    public AffiliateItem() { }
    public AffiliateItem(AdtractionFeedProduct adtractionFeedProduct)
    {
        Provider = AffiliateProvider.Adtraction;
        if (!string.IsNullOrEmpty(adtractionFeedProduct.Name) && !string.IsNullOrEmpty(adtractionFeedProduct.TrackingUrl))
        {
            ItemInfo = new AffiliateItemInfo
            {
                Title = adtractionFeedProduct.Name,
                Url = adtractionFeedProduct.TrackingUrl,
                ProductName = adtractionFeedProduct.Name,
                ImageUrl = adtractionFeedProduct.ImageUrl,
                Description = adtractionFeedProduct.Description,
                Category = adtractionFeedProduct.Category,
                NewPrice = adtractionFeedProduct.Price,
                OldPrice = adtractionFeedProduct.OriginalPrice,
                InStock = adtractionFeedProduct.InStock,
                Brand = adtractionFeedProduct.Brand,
            };
        }
    }
    public AffiliateItem(PartnerAdsFeedProduct partnerAdsFeedProduct)
    {
        Provider = AffiliateProvider.PartnerAds;
        if (!string.IsNullOrEmpty(partnerAdsFeedProduct.ProductName) && !string.IsNullOrEmpty(partnerAdsFeedProduct.ProductUrl))
        {
            ItemInfo = new AffiliateItemInfo
            {
                Title = partnerAdsFeedProduct.ProductName,
                Url = partnerAdsFeedProduct.ProductUrl,
                ProductName = partnerAdsFeedProduct.ProductName,
                ImageUrl = partnerAdsFeedProduct.ImageUrl,
                Description = partnerAdsFeedProduct.Description,
                Category = partnerAdsFeedProduct.CategoryName,
                NewPrice = partnerAdsFeedProduct.NewPrice,
                OldPrice = partnerAdsFeedProduct.OldPrice,
                InStock = partnerAdsFeedProduct.InStock,
                Brand = partnerAdsFeedProduct.Brand,
            };
        }
    }
    public AffiliateProvider Provider { get; set; }
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
    public string? Brand { get; set; }
    public string ProductName { get; set; } = "";
}

public class AffiliateItemReference
{
    public AffiliateProvider Provider { get; set; }
    public AdtractionItemReference? Adtraction { get; set; }
    public PartnerAdsItemReference? PartnerAds { get; set; }
}

public class AdtractionItemReference
{
    public AdtractionItemReference() { }
    public AdtractionItemReference(AdtractionFeedProduct adtractionFeedProduct)
    {
        ProgramId = adtractionFeedProduct.ProgramId;
        FeedId = adtractionFeedProduct.FeedId;
        Sku = adtractionFeedProduct.Sku ?? "";
    }
    public int ProgramId { get; set; }
    public int FeedId { get; set; }
    public string Sku { get; set; } = "";
}

public class PartnerAdsItemReference
{
    public PartnerAdsItemReference() { }
    public PartnerAdsItemReference(PartnerAdsFeedProduct partnerAdsFeedProduct)
    {
        ProgramId = partnerAdsFeedProduct.ProgramId.ToString(); // TODO: why is this an int?
        ProductId = partnerAdsFeedProduct.ProductId ?? "";
    }
    public string ProgramId { get; set; } = "";
    public string ProductId { get; set; } = "";
}

public enum AffiliateProvider
{
    NoValue = 0,
    Adtraction = 1,
    PartnerAds = 2,
}