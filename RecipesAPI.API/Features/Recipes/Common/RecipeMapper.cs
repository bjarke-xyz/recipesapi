using AutoMapper;
using RecipesAPI.API.Features.Files.DAL;
using RecipesAPI.API.Features.Recipes.DAL;
using RecipesAPI.API.Features.Recipes.Graph;

namespace RecipesAPI.API.Features.Recipes.Common;

public static class RecipeMapper
{
    private static IMapper mapper = new MapperConfiguration(cfg =>
    {
        cfg.CreateMap<RecipeDto, Recipe>()
            .ForMember(e => e.Parts, opt => opt.MapFrom(dto => dto.Parts ?? new List<RecipePartDto>()))
            .ForMember(e => e.Slugs, opt => opt.MapFrom(dto => dto.Slugs ?? new List<string>()))
            .ForMember(e => e.EquipmentIds, opt => opt.MapFrom(dto => dto.EquipmentIds ?? new List<string>()))
            ;
        cfg.CreateMap<RecipePartDto, RecipePart>()
            .ForMember(e => e.Ingredients, opt => opt.MapFrom(dto => dto.Ingredients ?? new List<RecipeIngredientDto>()));
        cfg.CreateMap<RecipeIngredientDto, RecipeIngredient>();

        cfg.CreateMap<Recipe, RecipeDto>()
            .ForMember(dto => dto.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("O")))
            .ForMember(dto => dto.LastModifiedAt, opt => opt.MapFrom(src => src.LastModifiedAt.ToString("O")))
            .ForMember(dto => dto.ModeratedAt, opt => opt.MapFrom(src => src.ModeratedAt.HasValue ? src.ModeratedAt.Value.ToString("O") : null))
            .ForMember(dto => dto.Parts, opt => opt.MapFrom(src => src.Parts ?? new List<RecipePart>()))
            .ForMember(dto => dto.EquipmentIds, opt => opt.MapFrom(src => src.EquipmentIds ?? new List<string>()))
            ;
        cfg.CreateMap<RecipePart, RecipePartDto>()
            .ForMember(dto => dto.Ingredients, opt => opt.MapFrom(src => src.Ingredients ?? new List<RecipeIngredient>()))
            ;
        cfg.CreateMap<RecipeIngredient, RecipeIngredientDto>();

        cfg.CreateMap<RecipeInput, Recipe>();
        cfg.CreateMap<RecipePartInput, RecipePart>();
        cfg.CreateMap<RecipePartIngredientInput, RecipeIngredient>();

        cfg.CreateMap<ImageDimensionsDto, ImageDimensions>();
        cfg.CreateMap<ImageDimensionDto, ImageDimension>();

        cfg.CreateMap<RecipeRatingDto, RecipeRating>();
        cfg.CreateMap<RecipeRating, RecipeRatingDto>();
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

    public static ImageDimensions? MapDto(ImageDimensionsDto? dto)
    {
        if (dto == null) return null;
        var imageDimensions = mapper.Map<ImageDimensionsDto, ImageDimensions>(dto);
        return imageDimensions;
    }
    public static ImageDimension? MapDto(ImageDimensionDto? dto)
    {
        if (dto == null) return null;
        var imageDimensions = mapper.Map<ImageDimensionDto, ImageDimension>(dto);
        return imageDimensions;
    }

    public static RecipeDto Map(Recipe recipe)
    {
        return mapper.Map<Recipe, RecipeDto>(recipe);
    }

    public static Recipe MapInput(RecipeInput input)
    {
        return mapper.Map<RecipeInput, Recipe>(input);
    }

    public static RecipeRatingDto Map(RecipeRating rating)
    {
        return mapper.Map<RecipeRating, RecipeRatingDto>(rating);
    }

    public static RecipeRating MapDto(RecipeRatingDto dto)
    {
        return mapper.Map<RecipeRatingDto, RecipeRating>(dto);
    }
}