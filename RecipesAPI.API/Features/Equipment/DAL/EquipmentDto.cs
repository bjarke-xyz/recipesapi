using Google.Cloud.Firestore;
using RecipesAPI.API.Features.Admin.DAL;

namespace RecipesAPI.API.Features.Equipment.DAL;

[FirestoreData]
public class EquipmentItemDto
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = default!;

    [FirestoreProperty("title")]
    public string Title { get; set; } = default!;

    [FirestoreProperty("description")]
    public string? Description { get; set; } = default!;

    [FirestoreProperty("icon")]
    public string? Icon { get; set; } = default!;

    [FirestoreProperty("links")]
    public List<EquipmentLinkDto> Links { get; set; } = [];

    [FirestoreProperty("affiliateItemReferences")]
    public List<AffiliateItemReferenceDto> AffiliateItemReferences { get; set; } = [];
}

[FirestoreData]
public class EquipmentLinkDto
{
    [FirestoreProperty("url")]
    public string Url { get; set; } = default!;

    [FirestoreProperty("title")]
    public string Title { get; set; } = default!;
}