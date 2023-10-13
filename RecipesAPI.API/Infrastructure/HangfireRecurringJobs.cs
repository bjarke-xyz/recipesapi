using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Recipes.BLL;

namespace RecipesAPI.API.Infrastructure;

public class HangfireRecurringJobs : BackgroundService
{
    private readonly IConfiguration config;

    public HangfireRecurringJobs(IConfiguration config)
    {
        this.config = config;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Hangfire.RecurringJob.AddOrUpdate<RecipeService>(nameof(RecipeService.GetRecipe), s => s.GetRecipes(CancellationToken.None, null), "0 * * * *");
        Hangfire.RecurringJob.AddOrUpdate<AdtractionService>(nameof(AdtractionService.RefreshProductFeeds), s => s.RefreshProductFeeds("DK", config.GetValue<int>("AdtractionChannelId")), "5 4 * * 1");
        return Task.CompletedTask;
    }
}