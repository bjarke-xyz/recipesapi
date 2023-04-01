namespace RecipesAPI.API.Infrastructure;

public class RequestInfoService
{
    private DateTime lastRequest = DateTime.UtcNow;

    public void OnNewRequestReceived(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/healthcheck/ready"))
        {
            return;
        }
        lastRequest = DateTime.UtcNow;
    }

    public DateTime? GetLastRequest()
    {
        return lastRequest;
    }
}