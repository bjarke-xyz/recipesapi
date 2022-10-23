using AutoMapper;
using RecipesAPI.Recipes.DAL;
using RecipesAPI.Recipes.Graph;

namespace RecipesAPI.Recipes.Common;

public static class RecipeMapper
{
    private static IMapper mapper = new MapperConfiguration(cfg =>
    {
        cfg.CreateMap<RecipeDto, Recipe>();
        cfg.CreateMap<RecipePartDto, RecipePart>();
        cfg.CreateMap<RecipeIngredientDto, RecipeIngredient>();

        cfg.CreateMap<Recipe, RecipeDto>();
        cfg.CreateMap<RecipePart, RecipePartDto>();
        cfg.CreateMap<RecipeIngredient, RecipeIngredientDto>();

        cfg.CreateMap<RecipeInput, Recipe>();
        cfg.CreateMap<RecipePartInput, RecipePart>();
        cfg.CreateMap<RecipePartIngredientInput, RecipeIngredient>();
    }).CreateMapper();

    public static Recipe MapDto(RecipeDto dto)
    {
        return mapper.Map<RecipeDto, Recipe>(dto);
    }

    public static RecipeDto Map(Recipe recipe)
    {
        return mapper.Map<Recipe, RecipeDto>(recipe);
    }

    public static Recipe MapInput(RecipeInput input)
    {
        return mapper.Map<RecipeInput, Recipe>(input);
    }
}