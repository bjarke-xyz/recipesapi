using Microsoft.AspNetCore.Mvc;
using RecipesAPI.API.Features.Files.BLL;
using RecipesAPI.API.Features.Files.DAL;
using RecipesAPI.API.Features.Recipes.BLL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Recipes.Controllers;

public class RecipesController : BaseController
{
    private readonly ILogger<RecipesController> logger;
    private readonly RecipeService recipeService;
    private readonly IFileService fileService;
    private readonly ImageProcessingService imageProcessingService;

    public RecipesController(ILogger<RecipesController> logger, RecipeService recipeService, IFileService fileService, ImageProcessingService imageProcessingService)
    {
        this.logger = logger;
        this.recipeService = recipeService;
        this.fileService = fileService;
        this.imageProcessingService = imageProcessingService;
    }

    [HttpGet("thumbnail/{recipeId}")]
    public async Task<IActionResult> GetThumbnail(CancellationToken cancellationToken, [FromRoute] string recipeId, [FromQuery] ThumbnailSize thumbnailSize = ThumbnailSize.Medium)
    {
        var recipe = await recipeService.GetRecipe(recipeId, cancellationToken, null);
        if (recipe == null) return NotFound("Recipe not found");
        if (string.IsNullOrEmpty(recipe.ImageId)) return NotFound("Recipe has no image");
        var file = await fileService.GetFile(recipe.ImageId, cancellationToken);
        if (file == null) return NotFound("Image file not found");

        var thumbnail = file.GetImageThumbnail(thumbnailSize);
        if (thumbnail != null)
        {
            var publicUrl = fileService.GetPublicUrl(file.Bucket, thumbnail.Key, thumbnail.ContentType, file.GetFileHash());
            return Redirect(publicUrl);
        }
        else
        {
            try
            {
                await imageProcessingService.ProcessRecipeImage(recipe.ImageId, recipe.Id, thumbnailSize, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create {thumbnailSize} thumbnail for recipe {id}", thumbnailSize, recipe.Id);
                return StatusCode(500, "Failed to create thumbnail");
            }
            var updatedFile = await fileService.GetFile(recipe.ImageId, cancellationToken);
            if (updatedFile == null) return NotFound("Updated file not found");
            var updatedThumbnail = updatedFile.GetImageThumbnail(thumbnailSize);
            if (updatedThumbnail == null) return NotFound("failed to get updated thumbnail");

            var publicUrl = fileService.GetPublicUrl(file.Bucket, updatedThumbnail.Key, updatedThumbnail.ContentType, file.GetFileHash());
            return Redirect(publicUrl);
        }

    }
}