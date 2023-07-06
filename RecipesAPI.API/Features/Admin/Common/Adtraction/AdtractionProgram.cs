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
    public string LogoURL { get; set; }

    [JsonProperty("trackingURL")]
    public string TrackingURL { get; set; }

    [JsonProperty("categoryName")]
    public string CategoryName { get; set; }

    [JsonProperty("categoryId")]
    public int? CategoryId { get; set; }

    [JsonProperty("trackingType")]
    public int? TrackingType { get; set; }

    [JsonProperty("status")]
    public int? Status { get; set; }
}

