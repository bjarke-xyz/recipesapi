using Newtonsoft.Json;

namespace RecipesAPI.API.Food.Common;

public class FoodItem
{
    public int FoodId { get; set; }
    public FoodText FoodName { get; set; } = default!;
    public List<FoodParameter> Parameters { get; set; } = default!;
}

public class FoodText
{
    public FoodText(string da, string en)
    {
        Da = da;
        En = en;
    }
    public FoodText() { }
    public string Da { get; set; } = default!;
    public string En { get; set; } = default!;
}

public class FoodParameter
{
    public int ParameterId { get; set; }
    public FoodText ParameterName { get; set; } = default!;
    public double? Value { get; set; }
}