using RecipesAPI.API.Features.Admin.DAL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Admin.BLL;

public class SettingsService(SettingsRepository settingsRepository, ICacheProvider cache)
{
    private const string cacheKey = "SETTINGS:";
    public async Task<SettingsDto> GetSettings()
    {
        var cached = await cache.Get<SettingsDto>(cacheKey);
        if (cached == null)
        {
            cached = await settingsRepository.GetSettings();
            await cache.Put(cacheKey, cached);
        }
        return cached;
    }

    public async Task<SettingsDto> SetSettings(SettingsDto settings)
    {
        await settingsRepository.SetSettings(settings);
        var updatedSettings = await settingsRepository.GetSettings();
        await cache.Put(cacheKey, updatedSettings);
        return updatedSettings;
    }
}