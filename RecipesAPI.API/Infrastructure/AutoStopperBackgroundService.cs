namespace RecipesAPI.API.Infrastructure;

public class AutoStopperBackgroundService : BackgroundService
{
    private readonly RequestInfoService requestInfoService;
    private readonly ILogger<AutoStopperBackgroundService> logger;
    private PeriodicTimer? timer;

    private readonly bool enableAutostop;
    private readonly int autoStopMinutes;

    public AutoStopperBackgroundService(IConfiguration configuration, RequestInfoService requestInfoService, ILogger<AutoStopperBackgroundService> logger)
    {
        enableAutostop = configuration.GetValue<bool>("ENABLE_AUTOSTOP", false);
        autoStopMinutes = configuration.GetValue<int>("AUTOSTOP_MINUTES", 20);
        this.requestInfoService = requestInfoService;
        this.logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (enableAutostop == false) return Task.CompletedTask;
        timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        _ = Task.Run(DoAutoStop);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (timer == null) return;

        while (await timer.WaitForNextTickAsync())
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                await DoAutoStop();
            }
        }
    }

    private async Task DoAutoStop()
    {
        var lastRequest = requestInfoService.GetLastRequest();
        if (lastRequest == null) return;
        logger.LogInformation("Last request happened at {requestTime}", lastRequest.Value);
        var now = DateTime.UtcNow;
        var difference = now - lastRequest.Value;
        if (difference.TotalMinutes > autoStopMinutes)
        {
            logger.LogInformation("Last request happened at {requestTime}, which is more than {autostopMins} minutes ago. Shutting down...", lastRequest.Value, autoStopMinutes);
            await Serilog.Log.CloseAndFlushAsync();
            Environment.Exit(0);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }

}