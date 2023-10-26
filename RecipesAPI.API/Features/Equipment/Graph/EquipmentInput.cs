using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Equipment.Common;

namespace RecipesAPI.API.Features.Equipment.Graph;

public class EquipmentInput
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public List<EquipmentLink>? Links { get; set; }
}
