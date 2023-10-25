using Google.Cloud.Firestore;

namespace RecipesAPI.API.Features.Admin.DAL;

[FirestoreData]
public class AffiliateItemReferenceDto
{
    [FirestoreProperty("provider")]
    public string Provider { get; set; } = "";

    [FirestoreProperty("adtraction")]
    public AdtractionItemReferenceDto? Adtraction { get; set; }

    [FirestoreProperty("partnerAds")]
    public PartnerAdsItemReferenceDto? PartnerAds { get; set; }
}

[FirestoreData]
public class AdtractionItemReferenceDto
{
    [FirestoreProperty("programId")]
    public int ProgramId { get; set; }

    [FirestoreProperty("feedId")]
    public int FeedId { get; set; }

    [FirestoreProperty("sku")]
    public string Sku { get; set; } = "";
}

[FirestoreData]
public class PartnerAdsItemReferenceDto
{
    [FirestoreProperty("programId")]
    public string ProgramId { get; set; } = "";

    [FirestoreProperty("productId")]
    public string ProductId { get; set; } = "";
}