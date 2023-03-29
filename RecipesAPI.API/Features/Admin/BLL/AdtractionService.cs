using RecipesAPI.API.Features.Admin.Common.Adtraction;

namespace RecipesAPI.API.Features.Admin.BLL;

public class AdtractionService
{
    private readonly ILogger<AdtractionService> logger;
    private readonly string url;
    private readonly string apiKey;
    private readonly HttpClient httpClient;

    public AdtractionService(ILogger<AdtractionService> logger, string url, string apiKey, HttpClient httpClient)
    {
        this.logger = logger;
        this.url = url;
        this.apiKey = apiKey;
        this.httpClient = httpClient;
    }

    public async Task<AdtractionAccountBalance> GetBalance(string currency)
    {
        try
        {
            var resp = await httpClient.GetFromJsonAsync<AdtractionAccountBalance>($"{url}/v2/partner/balance/{currency}?token={apiKey}");
            if (resp == null) throw new Exception("Failed to deserialize balance response");
            return resp;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get balance");
            throw;
        }
    }

    public async Task<List<AdtractionApplication>> GetApplications()
    {
        try
        {
            var resp = await httpClient.GetFromJsonAsync<List<AdtractionApplication>>($"{url}/v2/partner/applications?token={apiKey}");
            if (resp == null) throw new Exception("Failed to deserialize applications response");
            return resp;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to get applications");
            throw;
        }
    }
}