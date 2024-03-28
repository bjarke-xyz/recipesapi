using DotNet.Testcontainers.Builders;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RecipesAPI.API.Features.Recipes.BLL;
using RecipesAPI.API.Features.Recipes.DAL;

namespace RecipesAPI.Tests.IntegrationTests;

public class RecipeTests
{
  [TestCase]
  public async Task TestTest()
  {
    var container = new ContainerBuilder()
      // remember to build Dockerfile.firebase using docker compose
      .WithImage("bjarkt/firebase_emulator")
      .WithPortBinding(4000, true)
      .WithPortBinding(8080, true)
      .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(4000)))
      .Build();

    await container.StartAsync()
      .ConfigureAwait(false);

    var httpClient = new HttpClient();

    var requestUri = new UriBuilder(Uri.UriSchemeHttp, container.Hostname, container.GetMappedPublicPort(4000), "/").Uri;

    var resp = await httpClient.GetStringAsync(requestUri)
      .ConfigureAwait(false);

    Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", $"{container.Hostname}:{container.GetMappedPublicPort(8080)}");
    var db = new FirestoreDbBuilder
    {
      ProjectId = "demo-recipesapi",
      EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly,
    }.Build();
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