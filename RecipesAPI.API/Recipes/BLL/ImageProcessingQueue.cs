using RecipesAPI.Infrastructure;

namespace RecipesAPI.Recipes.BLL;

public class ImageProcessingQueue : DefaultBackgroundTaskQueue
{
    public ImageProcessingQueue(int capacity) : base(capacity)
    {
    }
}