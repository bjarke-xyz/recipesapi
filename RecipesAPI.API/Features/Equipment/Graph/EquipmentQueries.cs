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