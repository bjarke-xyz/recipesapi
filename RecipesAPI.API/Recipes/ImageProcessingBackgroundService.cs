using RecipesAPI.Infrastructure;
using RecipesAPI.Recipes.BLL;

namespace RecipesAPI.Recipes.BackgroundServices;

public class ImageProcessingBackgroundService : BackgroundService
{
    private readonly ImageProcessingQueue taskQueue;
    private readonly ILogger<ImageProcessingBackgroundService> logger;

    public ImageProcessingBackgroundService(ImageProcessingQueue taskQueue, ILogger<ImageProcessingBackgroundService> logger)
    {
        this.taskQueue = taskQueue;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return ProcessTaskQueue(stoppingToken);
    }

    private async Task ProcessTaskQueue(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await taskQueue.Dequeue(stoppingToken);
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Task cancelled
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during queue work item execution");
            }
        }
    }
}