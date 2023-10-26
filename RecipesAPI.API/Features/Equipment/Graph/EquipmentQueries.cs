using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Equipment.BLL;
using RecipesAPI.API.Features.Equipment.Common;

namespace RecipesAPI.API.Features.Equipment.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class EquipmentQueries
{
    public async Task<List<EquipmentItem>> GetEquipments([Service] EquipmentService service, CancellationToken cancellationToken)
    {
        var equipment = await service.GetEquipment(cancellationToken);
        return equipment;
    }

    public async Task<EquipmentItem?> GetEquipment(string id, [Service] EquipmentService service, CancellationToken cancellationToken)
    {
        var equipment = await service.GetEquipment(id, cancellationToken);
        return equipment;
    }
}

[ExtendObjectType(typeof(EquipmentItem))]
public class ExtendedEquipmentQueries
{
    public async Task<List<AffiliateItem>> GetAffiliateItems([Parent] EquipmentItem equipmentItem, AffiliateSearchDataLoader affiliateSearchDataLoader)
    {
        if (equipmentItem.AffiliateItemReferences?.Count > 0)
        {
            return [];
        }
        var affiliateItems = await affiliateSearchDataLoader.LoadAsync(equipmentItem.Title);
        return affiliateItems;
    }
}