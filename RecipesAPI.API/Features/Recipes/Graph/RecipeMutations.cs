using System.Dynamic;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Hangfire;
using Newtonsoft.Json;
using RecipesAPI.API.Auth;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Files.BLL;
using RecipesAPI.API.Features.Files.DAL;
using RecipesAPI.API.Features.Graph;
using RecipesAPI.API.Features.Ratings.BLL;
using RecipesAPI.API.Features.Ratings.Common;
using RecipesAPI.API.Features.Recipes.BLL;
using RecipesAPI.API.Features.Recipes.Common;
using RecipesAPI.API.Features.Users.Common;
using RecipesAPI.API.Infrastructure;
using RecipesAPI.API.Utils;

namespace RecipesAPI.API.Features.Recipes.Graph;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class RecipeMutations
{

    private static IReadOnlySet<string> allowedContentTypes = new HashSet<string> { "image/jpeg", "image/jpg", "image/png", "image/svg+xml", "image/gif" };

    private static async Task<string> UploadImage(string fileCode, string recipeId, ThumbnailSize thumbnailSize, IFileService fileService, ImageProcessingService imageProcessingService, CancellationToken cancellationToken)
    {
        var uploadUrlTicket = await fileService.GetUploadUrlTicket(fileCode);
        if (uploadUrlTicket == null)
        {
            throw new GraphQLErrorException($"File code ({fileCode}) was invalid");
        }

        var fileContent = await fileService.GetFileContent(uploadUrlTicket.Bucket, uploadUrlTicket.Key, cancellationToken);
        if (fileContent == null)
        {
            throw new GraphQLErrorException($"Failed to get uploaded file");
        }

        var fileDto = new FileDto
        {
            Id = uploadUrlTicket.FileId,
            Bucket = uploadUrlTicket.Bucket,
            Key = uploadUrlTicket.Key,
            FileName = uploadUrlTicket.FileName,
            Size = fileContent.Length,
            ContentType = uploadUrlTicket.ContentType,
        };
        try
        {
            await fileService.SaveFile(fileDto, cancellationToken);
#pragma warning disable 4014 // Hangfire awaits the method
            BackgroundJob.Enqueue<ImageProcessingService>(s => s.ProcessRecipeImage(fileDto.Id, recipeId, thumbnailSize, CancellationToken.None));
#pragma warning restore 4014
            return fileDto.Id;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException($"Failed to save image: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Upload flow:
    /// <para>1. User submits form</para>
    /// <para>2. Call CreateUploadUrl with details from user's file/image</para>
    /// <para>3. PUT file to UploadUrl (ContentType and ContentLength must match what was provided in CreateUploadUrl)</para>
    /// <para>4. Call CreateRecipe/UpdateRecipe with the FileId returned by CreateUploadUrl</para>
    /// </summary>
    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<UploadUrlPayload> CreateUploadUrl(CreateUploadUrlInput input, [Service] IFileService fileService, CancellationToken cancellationToken)
    {
        // 5mb
        if (input.ContentLength > 5_242_880)
        {
            throw new GraphQLErrorException("image must be less than 5mb");
        }
        if (!allowedContentTypes.Contains(input.ContentType))
        {
            throw new GraphQLErrorException("Image was not an image");
        }
        var fileId = Guid.NewGuid().ToString();
        var uploadTicket = await fileService.GetSignedUploadUrl($"images/{fileId}", fileId, input.ContentType, (ulong)input.ContentLength, input.FileName, cancellationToken);

        return new UploadUrlPayload
        {
            FileCode = uploadTicket.Code,
            Url = uploadTicket.Url,
        };
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<Recipe> CreateRecipe(RecipeInput input, [User] User loggedInUser, [Service] RecipeService recipeService, [Service] IFileService fileService, [Service] ImageProcessingService imageProcessingService, CancellationToken cancellationToken)
    {
        var thumbnailSize = ThumbnailSize.Medium;
        var recipe = RecipeMapper.MapInput(input);
        recipe.UserId = loggedInUser.Id;
        if (recipe.Slugs == null) recipe.Slugs = new List<string>();
        if (!string.IsNullOrEmpty(input.Slug) && loggedInUser.HasRole(Role.MODERATOR))
        {
            recipe.Slugs.Add(input.Slug);
        }
        if (loggedInUser.HasRole(Role.MODERATOR))
        {
            recipe.ModeratedAt = DateTime.UtcNow;
        }
        var createdRecipe = await recipeService.CreateRecipe(recipe, cancellationToken);
        string? imageId = null;
        if (input.FileCode != null)
        {
            imageId = await UploadImage(input.FileCode, createdRecipe.Id, thumbnailSize, fileService, imageProcessingService, cancellationToken);
        }
        createdRecipe.ImageId = imageId;
        var updatedRecipe = await recipeService.UpdateRecipe(createdRecipe, cancellationToken);
        return updatedRecipe;
    }

    private async Task<IDictionary<string, object>?> GetRawInput(HttpContext httpContext, string[] keys)
    {
        using var ms = new MemoryStream();
        httpContext.Request.Body.Position = 0;
        await httpContext.Request.Body.CopyToAsync(ms);
        var rawBodyStr = Encoding.UTF8.GetString(ms.ToArray());
        var rawBody = JsonConvert.DeserializeObject<ExpandoObject>(rawBodyStr);
        if (rawBody == null)
        {
            throw new GraphQLErrorException("Failed to get body");
        }
        var rawBodyDict = rawBody.ToDictionary(x => x.Key);
        var tmpDict = rawBodyDict;
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            if (i + 1 != keys.Length)
            {
                if (!tmpDict.TryGetValue(key, out var val))
                {
                    throw new GraphQLErrorException($"Property {key} not found");
                }
                tmpDict = (val.Value as ExpandoObject)?.ToDictionary(x => x.Key);
                if (tmpDict == null)
                {
                    throw new GraphQLErrorException($"Property {key} invalid format");
                }
            }
        }
        var lastKey = keys.Last();
        tmpDict.TryGetValue(lastKey, out var resultKvp);
        var resultDict = resultKvp.Value as IDictionary<string, object>;
        return resultDict;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<Recipe> UpdateRecipe(string id, bool? unpublish, RecipeInput input, [Service] IHttpContextAccessor httpContextAccessor, [User] User loggedInUser, [Service] RecipeService recipeService, [Service] IFileService fileService, [Service] ImageProcessingService imageProcessingService, CancellationToken cancellationToken)
    {
        var thumbnailSize = ThumbnailSize.Medium;
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) throw new GraphQLErrorException("Something went wrong");

        var inputVariablesDict = await GetRawInput(httpContext, new[] { "variables", "input" });
        if (inputVariablesDict == null)
        {
            throw new GraphQLErrorException("Failed to get input variables");
        }

        var existingRecipe = await recipeService.GetRecipe(id, cancellationToken, loggedInUser);
        if (existingRecipe == null)
        {
            throw new RecipeNotFoundException(id);
        }
        if (existingRecipe.UserId != loggedInUser.Id)
        {
            if (!loggedInUser.HasRole(Role.MODERATOR))
            {
                throw new GraphQLErrorException("You do not have permission to edit this recipe");
            }
        }

        var imageId = existingRecipe.ImageId;
        // If a FileCode is provided, upload and store image id
        if (input.FileCode != null)
        {
            imageId = await UploadImage(input.FileCode, existingRecipe.Id, thumbnailSize, fileService, imageProcessingService, cancellationToken);
        }
        // If FileCode property was provided, and it was set to null, delete image
        else if (input.FileCode == null && inputVariablesDict.Keys.Contains(nameof(RecipeInput.FileCode), StringComparer.OrdinalIgnoreCase))
        {
            imageId = null;
        }

        var recipe = RecipeMapper.MapInput(input);
        recipe.Id = existingRecipe.Id;
        recipe.CreatedAt = existingRecipe.CreatedAt;
        recipe.UserId = existingRecipe.UserId;
        recipe.Slugs = existingRecipe.Slugs;
        if (!string.IsNullOrEmpty(input.Slug) && !recipe.Slugs.Contains(input.Slug, StringComparer.OrdinalIgnoreCase) && loggedInUser.HasRole(Role.MODERATOR))
        {
            if (recipe.Slugs == null) recipe.Slugs = new List<string>();
            recipe.Slugs.Add(input.Slug);
        }
        if (loggedInUser.HasRole(Role.MODERATOR))
        {
            recipe.ModeratedAt = DateTime.UtcNow;
        }
        recipe.ImageId = imageId;
        if (unpublish.HasValue && unpublish.Value)
        {
            recipe.Published = false;
        }
        else
        {
            if (!recipe.Published && existingRecipe.Published)
            {
                existingRecipe.Draft = recipe.DeepClone();
                recipe = existingRecipe;
            }
            else
            {
                recipe.Draft = null;
            }
        }
        var updatedRecipe = await recipeService.UpdateRecipe(recipe, cancellationToken);
        return updatedRecipe;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<bool> DeleteRecipe(string id, [User] User loggedInUser, [Service] RecipeService recipeService, [Service] IFileService fileService, CancellationToken cancellationToken)
    {
        var recipe = await recipeService.GetRecipe(id, cancellationToken, loggedInUser);
        if (recipe == null)
        {
            throw new RecipeNotFoundException(id);
        }
        if (recipe.UserId != loggedInUser.Id)
        {
            if (!loggedInUser.HasRole(Role.MODERATOR))
            {
                throw new GraphQLErrorException("You do not have permission to edit this recipe");
            }
        }

        try
        {
            await recipeService.DeleteRecipe(recipe, cancellationToken);
        }
        catch
        {
            throw new GraphQLErrorException("Could not delete recipe");
        }


        if (!string.IsNullOrEmpty(recipe.ImageId))
        {
            try
            {
                var fileDto = await fileService.GetFile(recipe.ImageId, cancellationToken);
                if (fileDto != null)
                {
                    await fileService.DeleteFile(fileDto, cancellationToken);
                }
            }
            catch
            {
                throw new GraphQLErrorException("Could not delete recipe image");
            }
        }

        return true;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<bool> AddRating(string id, RateRecipeInput input, [User] User loggedInUser, [Service] RecipeService recipeService, [Service] RatingsService ratingsService, CancellationToken cancellationToken)
    {
        var recipe = await recipeService.GetRecipe(id, cancellationToken, loggedInUser);
        if (recipe == null)
        {
            throw new RecipeNotFoundException(id);
        }

        var rating = await ratingsService.GetRating(RatingType.Recipe, recipe.Id, loggedInUser.Id, cancellationToken);
        if (rating == null)
        {
            rating = new Rating
            {
                EntityType = RatingType.Recipe,
                EntityId = recipe.Id,
                UserId = loggedInUser.Id,
            };
        }
        rating.Score = input.Score;
        rating.Comment = input.Comment;

        await ratingsService.SaveRating(rating, cancellationToken);

        try
        {
            await recipeService.UpdateRating(recipe, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException("Failed to update recipe rating", ex);
        }

        return true;
    }
}