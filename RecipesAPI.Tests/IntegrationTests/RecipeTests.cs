using DotNet.Testcontainers.Builders;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RecipesAPI.API.Features.Recipes.BLL;
using RecipesAPI.API.Features.Recipes.DAL;

namespace RecipesAPI.Tests.IntegrationTests;

[TestFixture]
public class RecipeTests
{
  [SetUp]
  public async Task SetUp()
  {
    await FirebaseTestHelper.ClearFirestore();
  }
  [Test]
  public async Task TestTest()
  {
    var db = FirebaseTestHelper.GetDb();
    var recipeRepository = new RecipeRepository(db, NullLoggerFactory.Instance.CreateLogger<RecipeRepository>());
    var recipeId = Guid.NewGuid().ToString();
    await recipeRepository.SaveRecipe(new API.Features.Recipes.Common.Recipe
    {
      Id = recipeId,
      Title = "test"
    }, CancellationToken.None);
    var recipe = await recipeRepository.GetRecipe(recipeId, CancellationToken.None);

    Assert.That(recipe, Is.Not.Null);
    Assert.That(recipe.Id, Is.EqualTo(recipeId));
  }
}