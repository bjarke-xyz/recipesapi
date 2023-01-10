
namespace RecipesAPI.API.Features.Equipment.Common;

public class EquipmentItem
{
    public string Id { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string? Description { get; set; } = default!;

    public string? Icon { get; set; } = default!;

    public List<EquipmentLink> Links { get; set; } = new List<EquipmentLink>();
}

public class EquipmentLink
{
    public string Url { get; set; } = default!;

    public string Title { get; set; } = default!;
}