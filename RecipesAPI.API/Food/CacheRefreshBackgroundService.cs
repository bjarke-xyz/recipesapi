namespace RecipesAPI.Food;

public class CacheRefreshBackgroundService : BackgroundService
{
    private readonly ILogger<CacheRefreshBackgroundService> logger;
    private readonly FoodService foodService;

    public CacheRefreshBackgroundService(FoodService foodService, ILogger<CacheRefreshBackgroundService> logger)
    {
        this.foodService = foodService;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("getting food data");
        try
        {
            return this.foodService.GetFoodData(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "cache refresh service failed");
            return Task.CompletedTask;
        }
        finally
        {
            logger.LogInformation("getting food data finished");
        }
    }
}