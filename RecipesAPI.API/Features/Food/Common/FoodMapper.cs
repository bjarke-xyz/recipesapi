using AutoMapper;
using RecipesAPI.API.Features.Food.DAL;

namespace RecipesAPI.API.Features.Food.Common;

public static class FoodMapper
{
    public static List<FoodItem> MapDtos(List<FoodDto> dtos)
    {
        var result = new List<FoodItem>();
        var dtoById = dtos.GroupBy(x => x.FoodId).ToDictionary(x => x.Key, x => x.ToList());
        foreach (var kvp in dtoById)
        {
            var dto = kvp.Value.First();
            var foodItem = new FoodItem
            {
                FoodId = dto.FoodId,
                FoodName = new FoodText(dto.FoodNameDanish, dto.FoodName),
                Parameters = new List<FoodParameter>(),
            };
            foreach (var parameter in kvp.Value)
            {
                foodItem.Parameters.Add(new FoodParameter
                {
                    ParameterId = parameter.ParameterId,
                    ParameterName = new FoodText(parameter.ParameterNameDanish, parameter.ParameterName),
                    Value = parameter.ResVal,
                });
            }
            foodItem.Parameters = foodItem.Parameters.OrderBy(x => x.ParameterId).ToList();
            result.Add(foodItem);
        }
        result = result.OrderBy(x => x.FoodId).ToList();
        return result;
    }
}