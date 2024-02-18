using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Equipment.BLL;
using RecipesAPI.API.Features.Files.BLL;
using RecipesAPI.API.Features.Food.BLL;
using RecipesAPI.API.Features.Ratings.BLL;
using RecipesAPI.API.Features.Recipes.Common;
using RecipesAPI.API.Features.Users.BLL;

namespace RecipesAPI.API.Features.Recipes.BLL;

public class CacheJobService(
    ILogger<CacheJobService> logger,
    RecipeService recipeService,
    RatingsService ratingsService,
    UserService userService,
    IFileService fileService,
    ImageService imageService,
    EquipmentService equipmentService,
    FoodService foodService,
    AffiliateService affiliateService,
    IWebHostEnvironment env
)
{
    private readonly HttpClient httpClient = new HttpClient(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 10
    });
    private async Task GetThumbnail(ImageThumbnail? thumbnail, CancellationToken cancellationToken)
    {
        if (thumbnail == null) return;
        await httpClient.GetAsync(thumbnail.Src, cancellationToken);
    }
    public async Task RunRecipeCacheJob(CancellationToken cancellationToken)
    {
        try
        {
            var recipes = await recipeService.GetRecipes(cancellationToken, null);
            foreach (var recipe in recipes)
            {
                await ratingsService.GetComments(Ratings.Common.RatingType.Recipe, recipe.Id, cancellationToken);
                await ratingsService.GetReactions(Ratings.Common.RatingType.Recipe, recipe.Id, cancellationToken);
                await userService.GetUserById(recipe.UserId, cancellationToken);
                if (!string.IsNullOrEmpty(recipe.ImageId))
                {
                    var file = await fileService.GetFile(recipe.ImageId, cancellationToken);
                    if (file != null)
                    {
                        var img = imageService.GetImage(recipe, file);
                        if (img != null)
                        {
                            // No reason to do this in dev
                            // Does not hit cloudflare cache, and only uses up storage bandwidth
                            if (!env.IsDevelopment())
                            {
                                _ = httpClient.GetAsync(img.Src, cancellationToken);
                                _ = GetThumbnail(img.Thumbnails?.Small, cancellationToken);
                                _ = GetThumbnail(img.Thumbnails?.Medium, cancellationToken);
                                _ = GetThumbnail(img.Thumbnails?.Large, cancellationToken);
                            }
                        }
                    }
                }
                if (recipe.EquipmentIds?.Count > 0)
                {
                    await equipmentService.GetEquipmentByIds(recipe.EquipmentIds, cancellationToken);
                }

                foreach (var part in recipe.Parts ?? [])
                {
                    foreach (var ingredient in part.Ingredients ?? [])
                    {
                        await foodService.SearchFoodData(foodService.GetSearchQuery(ingredient), cancellationToken);
                        await affiliateService.SearchAffiliateItems(ingredient.Title, count: 5);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error in recipe cache job");
        }
    }
}