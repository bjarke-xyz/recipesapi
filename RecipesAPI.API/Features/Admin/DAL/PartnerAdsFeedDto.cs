using RecipesAPI.API.Features.Admin.Common;

namespace RecipesAPI.API.Features.Admin.DAL;

public class PartnerAdsProductFeedDto
{
    public PartnerAdsProductFeedDto() { }
    public PartnerAdsProductFeedDto(string? programId, string? feedLink, DateTime? feedUpdated)
    {
        ArgumentNullException.ThrowIfNull(programId);
        ArgumentNullException.ThrowIfNull(feedLink);
        ArgumentNullException.ThrowIfNull(feedUpdated);
        ProgramId = programId;
        FeedLink = feedLink;
        FeedUpdated = feedUpdated.Value;
    }

    public int Id { get; set; }
    public string ProgramId { get; set; } = "";
    public string FeedLink { get; set; } = "";
    public DateTime FeedUpdated { get; set; }
}

public class PartnerAdsProductFeedItemDto
{
    public PartnerAdsProductFeedItemDto() { }
    public PartnerAdsProductFeedItemDto(int partnerAdsProductFeedId, PartnerAdsFeedProduct product)
    {
        PartnerAdsProductFeedId = partnerAdsProductFeedId;
        Retailer = product.Retailer;
        CategoryName = product.CategoryName;
        Brand = product.Brand;
        ProductName = product.ProductName;
        ProductId = product.ProductId;
        Description = product.Description;
        NewPrice = product.NewPrice;
        OldPrice = product.OldPrice;
        DeliveryCost = product.DeliveryCost;
        StockQuantity = product.StockQuantity;
        DeliveryTime = product.DeliveryTime;
        Size = product.Size;
        ImageUrl = product.ImageUrl;
        ProductUrl = product.ProductUrl;
        ProgramId = product.ProgramId;
    }
    public int PartnerAdsProductFeedId { get; set; }
    public int ProgramId { get; set; }

    public string? Retailer { get; set; }
    public string? CategoryName { get; set; }
    public string? Brand { get; set; }
    public string? ProductName { get; set; }
    public string? ProductId { get; set; }
    public string? Description { get; set; }
    public decimal? NewPrice { get; set; }
    public decimal? OldPrice { get; set; }
    public string? DeliveryCost { get; set; }
    public string? StockQuantity { get; set; }
    public string? DeliveryTime { get; set; }
    public string? Size { get; set; }
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }

    public PartnerAdsFeedProduct ToFeedProduct()
    {
        var feedProduct = new PartnerAdsFeedProduct
        {
            Retailer = Retailer,
            CategoryName = CategoryName,
            Brand = Brand,
            ProductName = ProductName,
            ProductId = ProductId,
            Description = Description,
            NewPrice = NewPrice,
            OldPrice = OldPrice,
            DeliveryCost = DeliveryCost,
            StockQuantity = StockQuantity,
            DeliveryTime = DeliveryTime,
            Size = Size,
            ImageUrl = ImageUrl,
            ProductUrl = ProductUrl,
            ProgramId = ProgramId,
        };
        return feedProduct;
    }
}