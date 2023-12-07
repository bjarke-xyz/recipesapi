using Google.Cloud.Firestore;
using RecipesAPI.API.Features.Admin.Common;

namespace RecipesAPI.API.Features.Admin.DAL;

public class SettingsRepository(FirestoreDb db)
{
    private const string settingsCollection = "settings";

    private const string settingsId = "SETTINGS";

    public async Task<SettingsDto> GetSettings()
    {
        var snapshot = await db.Collection(settingsCollection).Document(settingsId).GetSnapshotAsync();
        var dto = snapshot?.ConvertTo<SettingsDto>() ?? new();
        return dto;
    }

    public async Task SetSettings(SettingsDto settingsDto)
    {
        await db.Collection(settingsCollection).Document(settingsId).SetAsync(settingsDto);
    }
}

[FirestoreData]
public class SettingsDto
{
    [FirestoreProperty("partnerSettings")]
    public List<PartnerSettingsDto> PartnerSettings { get; set; } = [];
}

[FirestoreData]
public class PartnerSettingsDto
{
    [FirestoreProperty("provider")]
    public AffiliateProvider Provider { get; set; }

    [FirestoreProperty("providerId")]
    public string ProviderId { get; set; } = "";

    [FirestoreProperty("area")]
    public PartnerSettingsArea Area { get; set; }

    [FirestoreProperty("positiveTags")]
    public List<string> PositiveTags { get; set; } = [];

    [FirestoreProperty("negativeTags")]
    public List<string> NegativeTags { get; set; } = [];

    [FirestoreProperty("categories")]
    public List<string> Categories { get; set; } = [];
}

[Flags]
public enum PartnerSettingsArea : int
{
    NoValue = 0,
    Equipment = 1,
    Ingredients = 2,
}