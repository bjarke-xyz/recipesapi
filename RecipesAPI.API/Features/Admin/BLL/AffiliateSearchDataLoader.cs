using RecipesAPI.API.Features.Admin.Common;

namespace RecipesAPI.API.Features.Admin.BLL;

public class AffiliateEquipmentSearchDataLoader : BatchDataLoader<string, List<AffiliateItem>>
{
    private readonly AffiliateService affiliateService;

    public AffiliateEquipmentSearchDataLoader(AffiliateService affiliateService, IBatchScheduler batchScheduler, DataLoaderOptions? options = null) : base(batchScheduler, options)
    {
        this.affiliateService = affiliateService;
    }

    protected override async Task<IReadOnlyDictionary<string, List<AffiliateItem>>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        var dict = await affiliateService.SearchAffiliateItems(keys.ToList(), count: 5, area: DAL.PartnerSettingsArea.Equipment);
        return dict;
    }
}

public class AffiliateIngredientSearchDataLoader : BatchDataLoader<string, List<AffiliateItem>>
{
    private readonly AffiliateService affiliateService;

    public AffiliateIngredientSearchDataLoader(AffiliateService affiliateService, IBatchScheduler batchScheduler, DataLoaderOptions? options = null) : base(batchScheduler, options)
    {
        this.affiliateService = affiliateService;
    }

    protected override async Task<IReadOnlyDictionary<string, List<AffiliateItem>>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        var dict = await affiliateService.SearchAffiliateItems(keys.ToList(), count: 5, area: DAL.PartnerSettingsArea.Ingredients);
        return dict;
    }
}