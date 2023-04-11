using RecipesAPI.API.Features.Recipes.BLL;

namespace RecipesAPI.API.Infrastructure;

public class HangfireRecurringJobs : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Hangfire.RecurringJob.AddOrUpdate<RecipeService>(nameof(RecipeService.GetRecipe), s => s.GetRecipes(CancellationToken.None, null), "0 * * * *");
        return Task.CompletedTask;
    }
}