namespace RecipesAPI.API.Infrastructure;

public class SettingsService
{
    public IConfiguration Configuration { get; private set; }

    public SettingsService(IConfiguration configuration)
    {
        this.Configuration = configuration;
    }
}