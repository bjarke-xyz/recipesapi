using System.Reflection;
using System.Security.Claims;
using RecipesAPI.Auth;
using RecipesAPI.Exceptions;
using RecipesAPI.Files.BLL;
using RecipesAPI.Files.DAL;
using RecipesAPI.Graph;
using RecipesAPI.Infrastructure;
using RecipesAPI.Recipes.BLL;
using RecipesAPI.Recipes.Common;
using RecipesAPI.Users.Common;

namespace RecipesAPI.Recipes.Graph;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class RecipeMutations
{

    private static IReadOnlySet<string> allowedContentTypes = new HashSet<string> { "image/jpeg", "image/jpg", "image/png", "image/svg+xml", "image/gif" };

    private static async Task<string> UploadImage(string fileCode, IFileService fileService, IBackgroundTaskQueue backgroundTaskQueue, ImageProcessingService imageProcessingService, CancellationToken cancellationToken)
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
            await backgroundTaskQueue.QueueBackgroundWorkItem((CancellationToken ct) => imageProcessingService.ProcessRecipeImage(fileDto.Id, ct));
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
        var uploadTicket = await fileService.GetSignedUploadUrl("recipes-5000.appspot.com", $"images/{fileId}", fileId, input.ContentType, (ulong)input.ContentLength, input.FileName, cancellationToken);

        return new UploadUrlPayload
        {
            FileCode = uploadTicket.Code,
            Url = uploadTicket.Url,
        };
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<Recipe> CreateRecipe(RecipeInput input, [User] User loggedInUser, [Service] RecipeService recipeService, [Service] IFileService fileService, [Service] IBackgroundTaskQueue backgroundTaskQueue, [Service] ImageProcessingService imageProcessingService, CancellationToken cancellationToken)
    {
        string? imageId = null;
        if (input.FileCode != null)
        {
            imageId = await UploadImage(input.FileCode, fileService, backgroundTaskQueue, imageProcessingService, cancellationToken);
        }
        var recipe = RecipeMapper.MapInput(input);
        recipe.ImageId = imageId;
        recipe.UserId = loggedInUser.Id;
        if (!string.IsNullOrEmpty(input.Slug) && loggedInUser.HasRole(Role.MODERATOR))
        {
            if (recipe.Slugs == null) recipe.Slugs = new List<string>();
            recipe.Slugs.Add(input.Slug);
        }
        var createdRecipe = await recipeService.CreateRecipe(recipe, cancellationToken);
        return createdRecipe;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<Recipe> UpdateRecipe(string id, RecipeInput input, [User] User loggedInUser, [Service] RecipeService recipeService, [Service] IFileService fileService, [Service] IBackgroundTaskQueue backgroundTaskQueue, [Service] ImageProcessingService imageProcessingService, CancellationToken cancellationToken)
    {
        var existingRecipe = await recipeService.GetRecipe(id, cancellationToken, true, loggedInUser.Id);
        if (existingRecipe == null)
        {
            throw new GraphQLErrorException($"Recipe with id {id} not found");
        }
        if (existingRecipe.UserId != loggedInUser.Id)
        {
            if (!loggedInUser.HasRole(Role.MODERATOR))
            {
                throw new GraphQLErrorException("You do not have permission to edit this recipe");
            }
        }

        var imageId = existingRecipe.ImageId;
        if (input.FileCode != null)
        {
            imageId = await UploadImage(input.FileCode, fileService, backgroundTaskQueue, imageProcessingService, cancellationToken);
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
        recipe.ImageId = imageId;
        var updatedRecipe = await recipeService.UpdateRecipe(recipe, cancellationToken);
        return updatedRecipe;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<bool> DeleteRecipe(string id, [User] User loggedInUser, [Service] RecipeService recipeService, [Service] IFileService fileService, CancellationToken cancellationToken)
    {
        var recipe = await recipeService.GetRecipe(id, cancellationToken, true, loggedInUser.Id);
        if (recipe == null)
        {
            throw new GraphQLErrorException($"Recipe with id {id} not found");
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
}