namespace RecipesAPI.API.Features.Healthcheck.BLL;

public class HealthcheckService
{
    private bool ready = false;
    private readonly IHostApplicationLifetime applicationLifetime;

    public HealthcheckService(IHostApplicationLifetime applicationLifetime)
    {
        this.applicationLifetime = applicationLifetime;
    }

    public bool IsReady()
    {
        return ready;
    }

    public void SetReady(bool ready)
    {
        this.ready = ready;
    }

    public void StartShutdownListener()
    {
        this.applicationLifetime.ApplicationStopping.Register(HandleShutdown);
    }

    private void HandleShutdown()
    {
        this.SetReady(false);
    }

}