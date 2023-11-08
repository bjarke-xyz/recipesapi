using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace RecipesAPI.API.Features.Admin.Common.Adtraction;

public class ProgramCategory
{
    [JsonProperty("category")]
    public string? Category { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("value")]
    public double? Value { get; set; }
}

public class Commission
{
    [JsonProperty("id")]
    public long? Id { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("value")]
    public double? Value { get; set; }

    [JsonProperty("transactionType")]
    public int? TransactionType { get; set; }

    [JsonProperty("categories")]
    public List<ProgramCategory>? Categories { get; set; }
}

public class Feed
{
    [JsonProperty("feedUrl")]
    public string? FeedUrl { get; set; }

    [JsonProperty("lastUpdated")]
    public DateTime? LastUpdated { get; set; }

    [JsonProperty("numberOfProducts")]
    public int? NumberOfProducts { get; set; }

    [JsonProperty("feedId")]
    public int? FeedId { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    public int? ProgramId { get; set; }
}

public class PublicAdtractionProgram
{
    [JsonProperty("programId")]
    public int ProgramId { get; set; }

    [JsonProperty("programName")]
    public string? ProgramName { get; set; }

    [JsonProperty("feeds")]
    public List<Feed>? Feeds { get; set; }
}

public class AdtractionProgram
{
    [JsonProperty("programId")]
    public int ProgramId { get; set; }

    [JsonProperty("market")]
    public string? Market { get; set; }

    [JsonProperty("currency")]
    public string? Currency { get; set; }

    [JsonProperty("approvalStatus")]
    public int? ApprovalStatus { get; set; }

    [JsonProperty("ppcMarketing")]
    public int? PpcMarketing { get; set; }

    [JsonProperty("socialMarketing")]
    public int? SocialMarketing { get; set; }

    [JsonProperty("emailMarketing")]
    public int? EmailMarketing { get; set; }

    [JsonProperty("cashbackMarketing")]
    public int? CashbackMarketing { get; set; }

    [JsonProperty("couponMarketing")]
    public int? CouponMarketing { get; set; }

    [JsonProperty("programName")]
    public string? ProgramName { get; set; }

    [JsonProperty("programUrl")]
    public string? ProgramUrl { get; set; }

    [JsonProperty("currentSegment")]
    public string? CurrentSegment { get; set; }

    [JsonProperty("pendingActive")]
    public bool? PendingActive { get; set; }

    [JsonProperty("cookieDuration")]
    public int? CookieDuration { get; set; }

    [JsonProperty("adId")]
    public int? AdId { get; set; }

    [JsonProperty("commissions")]
    public List<Commission>? Commissions { get; set; }

    [JsonProperty("feeds")]
    public List<Feed>? Feeds { get; set; }

    [JsonProperty("logoURL")]
    public string? LogoURL { get; set; }

    [JsonProperty("trackingURL")]
    public string? TrackingURL { get; set; }

    [JsonProperty("categoryName")]
    public string? CategoryName { get; set; }

    [JsonProperty("categoryId")]
    public int? CategoryId { get; set; }

    [JsonProperty("trackingType")]
    public int? TrackingType { get; set; }

    [JsonProperty("status")]
    public int? Status { get; set; }
}

[XmlRoot("productFeed")]
public class AdtractionProductFeed
{
    [XmlElement("product")]
    public List<AdtractionFeedProduct> ProductFeed { get; set; } = new();
}

public class AdtractionFeedProduct
{
    [XmlElement("SKU")]
    public string? Sku { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public string? Shipping { get; set; }
    public string? Currency { get; set; }

    [XmlElement("Instock"), GraphQLIgnore]
    public string? InstockStr { get; set; }

    public bool InStock => string.Equals(InstockStr, "yes", StringComparison.OrdinalIgnoreCase);

    public string? ProductUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? TrackingUrl { get; set; }
    public string? Brand { get; set; }

    [XmlElement("OriginalPrice"), GraphQLIgnore]
    public string? OriginalPriceStr { get; set; }

    public decimal? OriginalPrice => decimal.TryParse(OriginalPriceStr, out _) ? decimal.Parse(OriginalPriceStr) : null;

    public string? Ean { get; set; }
    public string? ManufacturerArticleNumber { get; set; }

    [XmlElement("Extras"), GraphQLIgnore]
    public object? ExtrasXmlNodes { get; set; }

    public void SetExtrasFromXml()
    {
        var extras = new List<AdtractionFeedProductExtra>();
        try
        {
            if (ExtrasXmlNodes is not XmlNode[] xmlNodes)
            {
                Extras = extras;
                return;
            }
            var current = new AdtractionFeedProductExtra();
            foreach (var node in xmlNodes)
            {
                if (string.Equals(node.Name, "name", StringComparison.OrdinalIgnoreCase))
                {
                    current.Name = node.FirstChild?.Value;
                }
                else if (string.Equals(node.Name, "value", StringComparison.OrdinalIgnoreCase))
                {
                    current.Value = node.FirstChild?.Value;
                    extras.Add(current);
                    current = new AdtractionFeedProductExtra();
                }
            }
            Extras = extras;
        }
        catch
        {
            Extras = extras;
        }
    }

    public void SetExtrasFromJson(string? extrasJson)
    {
        try
        {
            Extras = JsonConvert.DeserializeObject<List<AdtractionFeedProductExtra>>(extrasJson ?? "[]") ?? new();
        }
        catch
        {
            Extras = new();
        }
    }

    [XmlIgnore]
    public List<AdtractionFeedProductExtra> Extras { get; private set; } = new();

    public int ProgramId { get; set; }
    public int FeedId { get; set; }
}

public class AdtractionFeedProductExtra
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}