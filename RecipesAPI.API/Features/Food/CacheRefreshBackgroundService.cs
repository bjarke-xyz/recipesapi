using RecipesAPI.API.Features.Food.BLL;
using RecipesAPI.API.Features.Healthcheck.BLL;

namespace RecipesAPI.API.Features.Food;

public class CacheRefreshBackgroundService : BackgroundService
{
    private readonly ILogger<CacheRefreshBackgroundService> logger;
    private readonly FoodService foodService;
    private readonly HealthcheckService healthcheckService;

    public CacheRefreshBackgroundService(FoodService foodService, ILogger<CacheRefreshBackgroundService> logger, HealthcheckService healthcheckService)
    {
        this.foodService = foodService;
        this.logger = logger;
        this.healthcheckService = healthcheckService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("getting food data");
        try
        {
            await this.foodService.GetFoodData(stoppingToken);
            this.healthcheckService.SetReady(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "cache refresh service failed");
        }
        finally
        {
            logger.LogInformation("getting food data finished");
        }
    }
}