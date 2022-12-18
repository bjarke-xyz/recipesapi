using RecipesAPI.API.Auth;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Equipment.BLL;
using RecipesAPI.API.Features.Equipment.Common;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Features.Equipment.Graph;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class EquipmentMutations
{
    [RoleAuthorize(RoleEnums = new[] { Role.MODERATOR })]
    public async Task<EquipmentItem> CreateEquipment(EquipmentInput input, [Service] EquipmentService equipmentService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(input.Title))
        {
            throw new GraphQLErrorException("Title must not be empty");
        }
        var equipment = EquipmentMapper.MapInput(input);
        var createdEquipment = await equipmentService.SaveEquipment(equipment, cancellationToken);
        return createdEquipment;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.MODERATOR })]
    public async Task<EquipmentItem> UpdateEquipment(string id, EquipmentInput input, [Service] EquipmentService equipmentService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(input.Title))
        {
            throw new GraphQLErrorException("Title must not be empty");
        }
        var existingEquipment = await equipmentService.GetEquipment(id, cancellationToken);
        if (existingEquipment == null)
        {
            throw new GraphQLErrorException($"Equipment id {id} not found");
        }
        var equipment = EquipmentMapper.MapInput(input);
        equipment.Id = id;
        var createdEquipment = await equipmentService.SaveEquipment(equipment, cancellationToken);
        return createdEquipment;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.MODERATOR })]
    public async Task<bool> DeleteEquipment(string id, [Service] EquipmentService equipmentService, CancellationToken cancellationToken)
    {
        var existingEquipment = await equipmentService.GetEquipment(id, cancellationToken);
        if (existingEquipment == null)
        {
            throw new GraphQLErrorException($"Equipment id {id} not found");
        }
        // TODO: Delete recipes using this equipment id
        await equipmentService.DeleteEquipment(existingEquipment, cancellationToken);
        return true;
    }
}