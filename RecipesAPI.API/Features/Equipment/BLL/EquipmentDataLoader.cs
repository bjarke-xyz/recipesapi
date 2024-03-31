using RecipesAPI.API.Features.Equipment.Common;

namespace RecipesAPI.API.Features.Equipment.BLL;

public class EquipmentDataLoader : BatchDataLoader<string, EquipmentItem>
{
    private readonly EquipmentService equipmentService;

    public EquipmentDataLoader(EquipmentService equipmentService, IBatchScheduler batchScheduler, DataLoaderOptions? options = null) : base(batchScheduler, options)
    {
        this.equipmentService = equipmentService;
    }

    protected override async Task<IReadOnlyDictionary<string, EquipmentItem>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        var equipment = await equipmentService.GetEquipmentByIdsV2(keys, cancellationToken);
        return equipment;
    }
}