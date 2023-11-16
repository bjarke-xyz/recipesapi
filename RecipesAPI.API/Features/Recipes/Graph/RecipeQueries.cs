using RecipesAPI.API.Utils;
using RecipesAPI.API.Auth;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Recipes.Common;
using RecipesAPI.API.Features.Users.Common;
using RecipesAPI.API.Features.Recipes.BLL;
using RecipesAPI.API.Features.Food.BLL;
using RecipesAPI.API.Features.Food.Common;
using RecipesAPI.API.Features.Files.BLL;
using RecipesAPI.API.Features.Users.BLL;
using RecipesAPI.API.Features.Equipment.Common;
using RecipesAPI.API.Features.Equipment.BLL;
using RecipesAPI.API.Features.Files.DAL;
using RecipesAPI.API.Infrastructure;
using RecipesAPI.API.Features.Ratings.BLL;
using RecipesAPI.API.Features.Ratings.Common;

namespace RecipesAPI.API.Features.Recipes.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class RecipeQueries
{
    public async Task<List<Recipe>> GetRecipes([User] User loggedInUser, [Service] RecipeService recipeService, CancellationToken cancellationToken, RecipeFilter? filter = null)
    {
        filter = filter ?? new RecipeFilter();
        var recipes = await recipeService.GetRecipes(cancellationToken, loggedInUser);

        recipes = recipes.Where(x => x.Published == (filter.Published ?? true)).ToList();
        recipes = recipes.Where(x => x.ModeratedAt.HasValue == (filter.IsModerated ?? true)).ToList();


        if (!string.IsNullOrEmpty(filter.OrderByProperty))
        {
            if (!ClassUtils.IsPropertyOf<Recipe>(filter.OrderByProperty, out var propertyInfo) || propertyInfo == null)
            {
                throw new GraphQLErrorException($"The property '{filter.OrderByProperty}' does not exist on Recipe");
            }
            recipes = recipes.AsQueryable().OrderBy(propertyInfo.Name, filter.OrderDesc ?? true).ToList();
        }

        recipes = recipes.Skip(filter.Skip ?? 0).Take(filter.Limit ?? recipes.Count).ToList();

        return recipes;
    }

    public async Task<Recipe?> GetRecipe(string? id, string? slugOrId, [User] User loggedInUser, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(slugOrId))
        {
            throw new GraphQLErrorException($"{nameof(slugOrId)} must be specified");
        }
        if (string.IsNullOrEmpty(slugOrId))
        {
            slugOrId = id;
        }
        if (string.IsNullOrEmpty(slugOrId))
        {
            throw new GraphQLErrorException($"{nameof(slugOrId)} was null");
        }
        var recipe = await recipeService.GetRecipeBySlug(slugOrId, cancellationToken, loggedInUser);
        if (recipe == null)
        {
            recipe = await recipeService.GetRecipe(slugOrId, cancellationToken, loggedInUser);
        }
        return recipe;
    }

    public async Task<List<Recipe>> GetRecipesByIds(List<string> slugsOrIds, [User] User loggedInUser, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        var recipes = new List<Recipe>();
        foreach (var slugOrId in slugsOrIds)
        {
            var recipe = await recipeService.GetRecipeBySlug(slugOrId, cancellationToken, loggedInUser);
            if (recipe == null)
            {
                recipe = await recipeService.GetRecipe(slugOrId, cancellationToken, loggedInUser);
            }
            if (recipe != null)
            {
                recipes.Add(recipe);
            }
        }
        return recipes;
    }

    public RecipeIngredient? ParseIngredient(string ingredient, [Service] ParserService parserService)
    {
        var parsedIngredient = parserService.Parse(ingredient);
        return parsedIngredient;
    }
}

[ExtendObjectType(typeof(RecipeIngredient))]
public class RecipeIngredientQueries
{
    private string getSearchQuery(RecipeIngredient recipeIngredient)
    {
        var query = recipeIngredient.Title;
        if (recipeIngredient.Meta != null && recipeIngredient.Meta.Any())
        {
            var percentage = recipeIngredient.Meta.FirstOrDefault(x => x.Contains("%"));
            if (!string.IsNullOrEmpty(percentage))
            {
                query = $"{query} {percentage}";
            }
        }
        return query ?? "";
    }
    public async Task<FoodItem?> GetFood([Parent] RecipeIngredient recipeIngredient, FoodDataLoader foodDataLoader, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recipeIngredient.Title)) return null;

        var foodData = await foodDataLoader.LoadAsync(getSearchQuery(recipeIngredient), cancellationToken);
        return foodData.FirstOrDefault();
    }

    public async Task<List<FoodItem>> GetFoods([Parent] RecipeIngredient recipeIngredient, FoodDataLoader foodDataLoader, CancellationToken cancellationToken, int skip = 0, int limit = 10)
    {
        if (string.IsNullOrEmpty(recipeIngredient.Title)) return new List<FoodItem>();

        var foodData = await foodDataLoader.LoadAsync(getSearchQuery(recipeIngredient), cancellationToken);
        return foodData.Skip(skip).Take(limit).ToList();
    }

}

[ExtendObjectType(typeof(Rating))]
public class ExtendedRecipeRatingQueries
{
    public async Task<RecipeAuthor?> GetUser([Parent] Rating extendedRecipeRating, UserDataLoader userDataLoader, CancellationToken cancellationToken)
    {
        var user = await userDataLoader.LoadAsync(extendedRecipeRating.UserId, cancellationToken);
        if (user == null) return null;
        return new RecipeAuthor { UserId = user.Id, DisplayName = user.DisplayName ?? "" };
    }
}
[ExtendObjectType(typeof(Reaction))]
public class ExtendedRecipeReactionQueries
{
    public async Task<RecipeAuthor?> GetUser([Parent] Reaction reaction, UserDataLoader userDataLoader, CancellationToken cancellationToken)
    {
        var user = await userDataLoader.LoadAsync(reaction.UserId, cancellationToken);
        if (user == null) return null;
        return new RecipeAuthor { UserId = user.Id, DisplayName = user.DisplayName ?? "" };
    }
}
[ExtendObjectType(typeof(Comment))]
public class ExtendedRecipeCommentQueries
{
    public async Task<RecipeAuthor?> GetUser([Parent] Comment comment, UserDataLoader userDataLoader, CancellationToken cancellationToken)
    {
        var user = await userDataLoader.LoadAsync(comment.UserId, cancellationToken);
        if (user == null) return null;
        return new RecipeAuthor { UserId = user.Id, DisplayName = user.DisplayName ?? "" };
    }
}

[ExtendObjectType(typeof(Recipe))]
public class ExtendedRecipeQueries
{

    [Obsolete]
    public async Task<ExtendedRecipeRating?> GetExtendedRating([Parent] Recipe recipe, [User] User? loggedInUser, RecipeRatingsDataLoader recipeRatingsDataLoader, CancellationToken cancellationToken)
    {
        var recipeRatings = await recipeRatingsDataLoader.LoadAsync(recipe.Id, cancellationToken);
        if (recipeRatings == null || recipeRatings.Count == 0)
        {
            return null;
        }
        return new ExtendedRecipeRating(recipeRatings, loggedInUser);
    }

    public async Task<List<Comment>> GetComments([Parent] Recipe recipe, [User] User? loggedInUser, RecipeCommentsDataLoader recipeCommentsDataLoader, CancellationToken cancellationToken)
    {
        var comments = await recipeCommentsDataLoader.LoadAsync(recipe.Id, cancellationToken) ?? [];
        return comments;
    }

    public async Task<RecipeReactions> GetRecipeReactions([Parent] Recipe recipe, [User] User? loggedInUser, RecipeReactionsDataLoader recipeReactionsDataLoader, CancellationToken cancellationToken)
    {
        var recipeReactionsList = (await recipeReactionsDataLoader.LoadAsync(recipe.Id, cancellationToken)) ?? [];
        var recipeReactions = new RecipeReactions(recipeReactionsList, loggedInUser);
        return recipeReactions;
    }

    public async Task<Image?> GetImage([Parent] Recipe recipe, [Service] IFileService fileService, FileDataLoader fileDataLoader, [Service] ImageProcessingService imageProcessingService, [Service] SettingsService settingsService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recipe.ImageId)) return null;
        var file = await fileDataLoader.LoadAsync(recipe.ImageId, cancellationToken);
        if (file == null) return null;
        ImageThumbnails? thumbnails = null;
        var originalDimensions = file.Dimensions?.Original;
        thumbnails = new ImageThumbnails();
        var sizes = new List<ThumbnailSize> { ThumbnailSize.Small, ThumbnailSize.Medium, ThumbnailSize.Large };
        foreach (var thumbnailSize in sizes)
        {
            var thumbnailDto = file.GetImageThumbnail(thumbnailSize);
            if (thumbnailDto == null && originalDimensions != null)
            {
                var baseUrl = settingsService.Configuration["ApiUrl"];
                var generateThumbnailApiUrl = $"{baseUrl}/api/recipes/thumbnail/{recipe.Id}?thumbnailSize={thumbnailSize}";
                var (width, height, _) = imageProcessingService.GetImageThumbnailDimensions(originalDimensions.Width, originalDimensions.Height, thumbnailSize);
                var thumbnail = new ImageThumbnail
                {
                    Size = 0,
                    Src = generateThumbnailApiUrl,
                    Type = file.ContentType,
                    ThumbnailSize = thumbnailSize,
                    Dimensions = new ImageDimension
                    {
                        Width = width,
                        Height = height,
                    },
                };
                thumbnails.SetThumbnail(thumbnailSize, thumbnail);
            }
            else if (thumbnailDto != null)
            {
                var thumbnailSrc = fileService.GetPublicUrl(file.Bucket, thumbnailDto.Key);
                var thumbnail = new ImageThumbnail
                {
                    Size = thumbnailDto.Size,
                    Type = thumbnailDto.ContentType,
                    Src = thumbnailSrc,
                    ThumbnailSize = thumbnailSize,
                    Dimensions = RecipeMapper.MapDto(thumbnailDto.Dimensions),
                };
                thumbnails.SetThumbnail(thumbnailSize, thumbnail);
            }
        }
        var imageSrc = fileService.GetPublicUrl(file);
        var image = new Image
        {
            ImageId = recipe.ImageId,
            Name = file.FileName,
            Size = file.Size,
            Type = file.ContentType,
            Src = imageSrc,
            Thumbnails = thumbnails,
            Dimensions = RecipeMapper.MapDto(file.Dimensions),
        };
        return image;
    }

    public async Task<RecipeAuthor> GetUser([Parent] Recipe recipe, UserDataLoader userDataLoader, CancellationToken cancellationToken)
    {
        var user = await userDataLoader.LoadAsync(recipe.UserId, cancellationToken);
        return new RecipeAuthor
        {
            DisplayName = user.DisplayName ?? "",
            UserId = recipe.UserId,
        };
    }

    public async Task<List<EquipmentItem>> GetEquipment([Parent] Recipe recipe, EquipmentDataLoader equipmentDataLoader, CancellationToken cancellationToken)
    {
        var equipment = await equipmentDataLoader.LoadAsync(recipe.EquipmentIds, cancellationToken);
        return equipment.Where(x => x != null).ToList();
    }

}
