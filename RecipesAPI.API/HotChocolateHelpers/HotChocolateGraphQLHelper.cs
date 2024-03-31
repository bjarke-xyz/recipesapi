using HotChocolate.Execution.Configuration;
using RecipesAPI.API.Features.Admin.Graph;
using RecipesAPI.API.Features.Equipment.Graph;
using RecipesAPI.API.Features.Food.Graph;
using RecipesAPI.API.Features.Recipes.Graph;
using RecipesAPI.API.Features.Users.Graph;

namespace RecipesAPI.API.HotChocolateHelpers;

public static class HotChocolateGraphQLHelper
{
    public static IRequestExecutorBuilder AddRecipesAPITypes(this IRequestExecutorBuilder builder)
    {
        return builder
        .AddAuthorization()
        .AddQueryType()
        .AddMutationType()
            // Users
            .AddTypeExtension<UserQueries>()
            .AddTypeExtension<ExtendedUserQueries>()
            .AddTypeExtension<ExtendedSimpleUserQueries>()
            .AddTypeExtension<UserMutations>()
            // Recipes
            .AddTypeExtension<RecipeQueries>()
            .AddTypeExtension<RecipeMutations>()
            .AddTypeExtension<RecipeIngredientQueries>()
            .AddTypeExtension<ExtendedRecipeQueries>()
            .AddTypeExtension<ExtendedRecipeRatingQueries>()
            .AddTypeExtension<ExtendedRecipeReactionQueries>()
            .AddTypeExtension<ExtendedRecipeCommentQueries>()
            // Food
            .AddTypeExtension<FoodQueries>()
            // Admin
            .AddTypeExtension<AdminQueries>()
            .AddTypeExtension<AdtractionFeedQueries>()
            .AddTypeExtension<PartnerAdsProgramQueries>()
            .AddTypeExtension<AffiliateItemReferenceQueries>()
            .AddTypeExtension<AdminMutations>()
            // Equipment
            .AddTypeExtension<EquipmentQueries>()
            .AddTypeExtension<ExtendedEquipmentQueries>()
            .AddTypeExtension<EquipmentMutations>()
        .AddType<UploadType>();
    }
}