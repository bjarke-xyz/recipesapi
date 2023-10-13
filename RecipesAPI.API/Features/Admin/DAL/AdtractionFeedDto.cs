using System.ComponentModel;
using Newtonsoft.Json;
using RecipesAPI.API.Features.Admin.Common.Adtraction;

namespace RecipesAPI.API.Features.Admin.DAL;

public class AdtractionProductFeedDto
{
    public AdtractionProductFeedDto() { }
    public AdtractionProductFeedDto(int programId, Feed feed)
    {
        ArgumentNullException.ThrowIfNull(feed.FeedId);
        ArgumentNullException.ThrowIfNull(feed.FeedUrl);
        ArgumentNullException.ThrowIfNull(feed.LastUpdated);
        ProgramId = programId;
        FeedId = feed.FeedId.Value;
        FeedUrl = feed.FeedUrl;
        LastUpdated = feed.LastUpdated.Value;
    }
    public int Id { get; set; }
    public int ProgramId { get; set; }
    public int FeedId { get; set; }
    public string FeedUrl { get; set; } = "";
    public DateTime LastUpdated { get; set; }
}

public class AdtractionProductFeedItemDto
{
    public AdtractionProductFeedItemDto() { }
    public AdtractionProductFeedItemDto(int adtractionProductFeedId, AdtractionFeedProduct product)
    {
        AdtractionProductFeedId = adtractionProductFeedId;
        Sku = product.Sku;
        Name = product.Name;
        Description = product.Description;
        Category = product.Category;
        Price = product.Price;
        Shipping = product.Shipping;
        Currency = product.Currency;
        InStock = product.InStock;
        ProductUrl = product.ProductUrl;
        ImageUrl = product.ImageUrl;
        TrackingUrl = product.TrackingUrl;
        Brand = product.Brand;
        OriginalPrice = product.OriginalPrice;
        Ean = product.Ean;
        ManufacturerArticleNumber = product.ManufacturerArticleNumber;
        ExtrasJson = JsonConvert.SerializeObject(product.Extras);
    }
    public int AdtractionProductFeedId { get; set; }
    public string? Sku { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public string? Shipping { get; set; }
    public string? Currency { get; set; }
    public bool InStock { get; set; } = false;
    public string? ProductUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? TrackingUrl { get; set; }
    public string? Brand { get; set; }
    public string? OriginalPrice { get; set; }
    public string? Ean { get; set; }
    public string? ManufacturerArticleNumber { get; set; }
    public string? ExtrasJson { get; set; }
    public AdtractionFeedProduct ToFeedProduct()
    {
        var feedProduct = new AdtractionFeedProduct
        {
            Sku = Sku,
            Name = Name,
            Description = Description,
            Category = Category,
            Price = Price,
            Shipping = Shipping,
            Currency = Currency,
            InstockStr = InStock ? "yes" : "no",
            ProductUrl = ProductUrl,
            ImageUrl = ImageUrl,
            TrackingUrl = TrackingUrl,
            Brand = Brand,
            OriginalPrice = OriginalPrice,
            Ean = Ean,
            ManufacturerArticleNumber = ManufacturerArticleNumber,
        };
        feedProduct.SetExtrasFromJson(ExtrasJson);
        return feedProduct;
    }
}