using System.Globalization;
using System.Xml.Serialization;

namespace RecipesAPI.API.Features.Admin.Common;

[XmlRoot("program")]
public class PartnerAdsProgram
{
    [XmlElement(ElementName = "programid")]
    public string ProgramId { get; set; } = "";

    [XmlElement(ElementName = "programnavn")]
    public string ProgramName { get; set; } = "";

    [XmlElement(ElementName = "programurl")]
    public string ProgramUrl { get; set; } = "";

    [XmlElement(ElementName = "programbeskrivelse")]
    public string ProgramDescription { get; set; } = "";

    [XmlElement(ElementName = "kategoriid")]
    public string CategoryId { get; set; } = "";

    [XmlElement(ElementName = "kategorinavn")]
    public string CategoryName { get; set; } = "";

    [XmlElement(ElementName = "underkategori")]
    public string SubCategory { get; set; } = "";

    [XmlElement(ElementName = "feed"), GraphQLIgnore]
    public string? FeedStr { get; set; }

    public bool Feed => string.Equals(FeedStr, "ja", StringComparison.OrdinalIgnoreCase);

    [XmlElement(ElementName = "kliksats")]
    public double ClickRate { get; set; }

    [XmlElement(ElementName = "leadsats")]
    public double LeadRate { get; set; }

    [XmlElement(ElementName = "provision")]
    public double Provision { get; set; }

    [XmlElement(ElementName = "Epc")]
    public string? Epc { get; set; }

    [XmlElement(ElementName = "SEM_PPC")]
    public string? SemPpc { get; set; }

    [XmlElement(ElementName = "SEM_PPC_restrik")]
    public string? SemPpcRestriction { get; set; }

    [XmlElement(ElementName = "ShoppingAds")]
    public string? ShoppingAds { get; set; }

    [XmlElement(ElementName = "ShoppingAds_restrik")]
    public string? ShoppingAdsRestriction { get; set; }

    [XmlElement(ElementName = "Social_PPC")]
    public string? SocialPpc { get; set; }

    [XmlElement(ElementName = "cashback")]
    public string? Cashback { get; set; }

    [XmlElement(ElementName = "rabatsites")]
    public string? Rabatsites { get; set; }

    [XmlElement(ElementName = "affiliatelink")]
    public string? AffiliateLink { get; set; }

    [XmlElement(ElementName = "kontaktperson")]
    public string? ContactPerson { get; set; }

    [XmlElement(ElementName = "email")]
    public string? Email { get; set; }

    [XmlElement(ElementName = "feedlink")]
    public string? FeedLink { get; set; }

    [XmlElement(ElementName = "status")]
    public string? Status { get; set; }

    [XmlElement("feedcur")]
    public string Currency { get; set; } = "";

    [XmlElement("feedmarket")]
    public string Market { get; set; } = "";

    [XmlElement("feedupdated"), GraphQLIgnore]
    public string? FeedUpdatedStr { get; set; }
    public DateTime? FeedUpdated
    {
        get
        {
            if (DateTime.TryParseExact(FeedUpdatedStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime))
            {
                return parsedDateTime;
            }
            return null;
        }
    }

}

[XmlRoot("partnerprogrammer")]
public class PartnerAdsPrograms
{
    [XmlElement(ElementName = "program")]
    public List<PartnerAdsProgram> Programs { get; set; } = new List<PartnerAdsProgram>();

}

[XmlRoot("produkt")]
public class PartnerAdsFeedProduct
{
    [XmlElement("forhandler")]
    public string? Retailer { get; set; }

    [XmlElement("kategorinavn")]
    public string? CategoryName { get; set; }

    [XmlElement("brand")]
    public string? Brand { get; set; }

    [XmlElement("produktnavn")]
    public string? ProductName { get; set; }

    [XmlElement("produktid")]
    public string? ProductId { get; set; }

    [XmlElement("beskrivelse")]
    public string? Description { get; set; }

    [XmlElement("nypris")]
    public decimal? NewPrice { get; set; }

    [XmlElement("glpris")]
    public decimal? OldPrice { get; set; }

    [XmlElement("fragtomk")]
    public string? DeliveryCost { get; set; }

    [XmlElement("lagerantal"), GraphQLIgnore]
    public string? StockQuantity { get; set; }
    private static readonly IReadOnlySet<string> inStockStrs = new HashSet<string>{
        "in stock", "in_stock"
    };
    public bool InStock => inStockStrs.Contains(StockQuantity, StringComparer.OrdinalIgnoreCase);

    [XmlElement("leveringstid")]
    public string? DeliveryTime { get; set; }

    [XmlElement("size")]
    public string? Size { get; set; }

    [XmlElement("billedurl")]
    public string? ImageUrl { get; set; }

    [XmlElement("vareurl")]
    public string? ProductUrl { get; set; }

    public int ProgramId { get; set; }
}

[XmlRoot("produkter")]
public class PartnerAdsProductFeed
{
    [XmlElement("produkt")]
    public List<PartnerAdsFeedProduct> Products { get; set; } = new();
}