using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Equipment.BLL;
using RecipesAPI.API.Features.Food.BLL;
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
        Hangfire.RecurringJob.AddOrUpdate<CacheJobService>(nameof(CacheJobService.RunRecipeCacheJob), s => s.RunRecipeCacheJob(CancellationToken.None), "*/10 * * * *");
        Hangfire.RecurringJob.AddOrUpdate<AffiliateService>(nameof(AffiliateService.RefreshProductFeeds), s => s.RefreshProductFeeds(CancellationToken.None), "0 * * * *");
        Hangfire.RecurringJob.AddOrUpdate<PartnerAdsService>("partnerads_" + nameof(PartnerAdsService.GetPrograms), s => s.GetPrograms(true), "0 * * * *");
        Hangfire.RecurringJob.AddOrUpdate<SqliteCacheProvider>(nameof(SqliteCacheProvider.RemoveExpired), s => s.RemoveExpired(), "* * * * *");
        Hangfire.RecurringJob.AddOrUpdate<EquipmentService>(nameof(EquipmentService.RunCacheJob), s => s.RunCacheJob(CancellationToken.None), "0 * * * *");

        Hangfire.RecurringJob.TriggerJob(nameof(AffiliateService.RefreshProductFeeds));
        Hangfire.RecurringJob.TriggerJob("partnerads_" + nameof(PartnerAdsService.GetPrograms));
        Hangfire.RecurringJob.TriggerJob(nameof(CacheJobService.RunRecipeCacheJob));

        Hangfire.BackgroundJob.Enqueue<FoodService>(s => s.BuildSearchIndex(CancellationToken.None));
        Hangfire.BackgroundJob.Enqueue<RecipeService>(s => s.BuildSearchIndex(CancellationToken.None));

        return Task.CompletedTask;
    }
}