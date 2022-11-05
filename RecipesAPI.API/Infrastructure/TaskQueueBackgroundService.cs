using RecipesAPI.Infrastructure;
using RecipesAPI.Recipes.BLL;

namespace RecipesAPI.Infrastructure;

public class TaskQueueBackgroundService : BackgroundService
{
    private readonly IBackgroundTaskQueue taskQueue;
    private readonly ILogger<TaskQueueBackgroundService> logger;

    public TaskQueueBackgroundService(IBackgroundTaskQueue taskQueue, ILogger<TaskQueueBackgroundService> logger)
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