using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Equipment.BLL;
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
        Hangfire.RecurringJob.AddOrUpdate<AdtractionService>("adtraction_" + nameof(AdtractionService.RefreshProductFeeds), s => s.RefreshProductFeeds("DK", config.GetValue<int>("AdtractionChannelId"), null, null), "0 * * * *");
        Hangfire.RecurringJob.AddOrUpdate<PartnerAdsService>("partnerads_" + nameof(PartnerAdsService.RefreshProductFeeds), s => s.RefreshProductFeeds(null, null), "0 * * * *");
        Hangfire.RecurringJob.AddOrUpdate<SqliteCacheProvider>(nameof(SqliteCacheProvider.RemoveExpired), s => s.RemoveExpired(), "* * * * *");
        Hangfire.RecurringJob.AddOrUpdate<EquipmentService>(nameof(EquipmentService.RunCacheJob), s => s.RunCacheJob(CancellationToken.None), "0 * * * *");

        Hangfire.RecurringJob.TriggerJob("adtraction_" + nameof(AdtractionService.RefreshProductFeeds));
        Hangfire.RecurringJob.TriggerJob("partnerads_" + nameof(PartnerAdsService.RefreshProductFeeds));

        return Task.CompletedTask;
    }
}