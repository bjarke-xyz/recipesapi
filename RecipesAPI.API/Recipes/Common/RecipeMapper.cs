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

        cfg.CreateMap<Recipe, RecipeDto>()
            .ForMember(dto => dto.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("O")))
            .ForMember(dto => dto.LastModifiedAt, opt => opt.MapFrom(src => src.LastModifiedAt.ToString("O")))
            .ForMember(dto => dto.ModeratedAt, opt => opt.MapFrom(src => src.ModeratedAt.HasValue ? src.ModeratedAt.Value.ToString("O") : null))
            ;
        cfg.CreateMap<RecipePart, RecipePartDto>();
        cfg.CreateMap<RecipeIngredient, RecipeIngredientDto>();

        cfg.CreateMap<RecipeInput, Recipe>();
        cfg.CreateMap<RecipePartInput, RecipePart>();
        cfg.CreateMap<RecipePartIngredientInput, RecipeIngredient>();
    }).CreateMapper();

    public static Recipe MapDto(RecipeDto dto, string docId)
    {
        var recipe = mapper.Map<RecipeDto, Recipe>(dto);
        if (string.IsNullOrEmpty(recipe.Id))
        {
            recipe.Id = docId;
        }
        return recipe;
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