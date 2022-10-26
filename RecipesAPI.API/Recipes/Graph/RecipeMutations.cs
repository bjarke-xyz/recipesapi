using System.Reflection;
using System.Security.Claims;
using RecipesAPI.Auth;
using RecipesAPI.Exceptions;
using RecipesAPI.Files.BLL;
using RecipesAPI.Files.DAL;
using RecipesAPI.Recipes.BLL;
using RecipesAPI.Recipes.Common;
using RecipesAPI.Users.Common;

namespace RecipesAPI.Recipes.Graph;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class RecipeMutations
{

    private static IReadOnlySet<string> allowedContentTypes = new HashSet<string> { "image/jpeg", "image/jpg", "image/png", "image/svg+xml", "image/gif" };

    private static string? GetContenType(IFile file)
    {
        // TODO: remove hack when HotChocolate 13 is released https://github.com/ChilliCream/hotchocolate/pull/5231
        var field = file.GetType().GetField("_file", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) return null;
        var iformFile = field.GetValue(file) as IFormFile;
        if (iformFile == null) return null;
        return iformFile.ContentType;
    }

    private static (bool valid, string contentType) ValidateImage(IFile file)
    {
        var contentType = GetContenType(file);
        if (string.IsNullOrEmpty(contentType))
        {
            throw new GraphQLErrorException("image had no content type");
        }
        if (!file.Length.HasValue)
        {
            throw new GraphQLErrorException("image had no length");
        }
        // 5mb
        if (file.Length.Value > 5_242_880)
        {
            throw new GraphQLErrorException("image must be less than 5mb");
        }
        if (!allowedContentTypes.Contains(contentType))
        {
            throw new GraphQLErrorException("Image was not an image");
        }
        return (true, contentType);
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<Recipe> CreateRecipe(RecipeInput input, [UserId] string userId, [Service] RecipeService recipeService, [Service] FileService fileService, CancellationToken cancellationToken)
    {
        if (input.Image != null)
        {
            var (_, contenType) = ValidateImage(input.Image);
            var fileId = Guid.NewGuid().ToString();
            var fileDto = new FileDto
            {
                Id = fileId,
                Bucket = "recipesapi",
                Key = $"images/{fileId}",
                FileName = input.Image.Name,
                Size = input.Image.Length!.Value,
                ContentType = contenType,
            };
            try
            {
                using var stream = input.Image.OpenReadStream();
                await fileService.SaveFile(fileDto, stream, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new GraphQLErrorException($"Failed to save image: {ex.Message}", ex);
            }
        }
        var recipe = RecipeMapper.MapInput(input);
        recipe.UserId = userId;
        var createdRecipe = await recipeService.CreateRecipe(recipe, cancellationToken);
        return createdRecipe;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<Recipe> UpdateRecipe(string id, RecipeInput input, [UserId] string userId, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        var existingRecipe = await recipeService.GetRecipe(id, cancellationToken, true);
        if (existingRecipe == null)
        {
            throw new GraphQLErrorException($"recipe with id {id} not found");
        }
        if (existingRecipe.UserId != userId)
        {
            if (!userRoles.Contains(Role.ADMIN))
            {
                throw new GraphQLErrorException("You do not have permission to edit this recipe");
            }
        }
        if (input.Image != null)
        {

        }

        var recipe = RecipeMapper.MapInput(input);
        recipe.Id = existingRecipe.Id;
        recipe.CreatedAt = existingRecipe.CreatedAt;
        recipe.UserId = existingRecipe.UserId;
        var updatedRecipe = await recipeService.UpdateRecipe(recipe, cancellationToken);
        return updatedRecipe;
    }
}