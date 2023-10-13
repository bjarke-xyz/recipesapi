using System.Net.Http.Headers;
using System.Xml.Serialization;
using Newtonsoft.Json;
using RecipesAPI.API.Features.Admin.Common.Adtraction;
using RecipesAPI.API.Features.Admin.DAL;

namespace RecipesAPI.API.Features.Admin.BLL;

public class AdtractionService
{
    private readonly ILogger<AdtractionService> logger;
    private readonly string url;
    private readonly string apiKey;
    private readonly HttpClient httpClient;
    private readonly AdtractionRepository adtractionRepository;

    public AdtractionService(ILogger<AdtractionService> logger, string url, string apiKey, HttpClient httpClient, AdtractionRepository adtractionRepository)
    {
        this.logger = logger;
        this.url = url;
        this.apiKey = apiKey;
        this.httpClient = httpClient;
        this.adtractionRepository = adtractionRepository;
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

    public async Task<List<AdtractionFeedProduct>> ParseProductFeed(string feedUrl)
    {
        var xmlStream = await httpClient.GetStreamAsync(feedUrl);
        var xmlSerializer = new XmlSerializer(typeof(AdtractionProductFeed));
        IEnumerable<AdtractionFeedProduct> feedProducts = (xmlSerializer.Deserialize(xmlStream) as AdtractionProductFeed ?? new()).ProductFeed ?? new();
        foreach (var p in feedProducts)
        {
            p.SetExtrasFromXml();
        }
        return feedProducts.ToList();
    }

    public async Task RefreshProductFeeds(string market, int channelId)
    {
        var programs = await GetPrograms(market, null, channelId, 1, null);
        foreach (var program in programs)
        {
            foreach (var feed in program.Feeds ?? new())
            {
                if (!feed.FeedId.HasValue || !feed.LastUpdated.HasValue || string.IsNullOrWhiteSpace(feed.FeedUrl))
                {
                    continue;
                }

                var productFeed = await ParseProductFeed(feed.FeedUrl);

                await adtractionRepository.SaveProductFeed(program.ProgramId, feed, productFeed);
            }
        }
    }

    public async Task<List<AdtractionFeedProduct>> GetFeedProducts(int? programId, int? feedId, int? skip, int? limit, string? searchQuery)
    {
        if (!programId.HasValue || !feedId.HasValue)
        {
            return new();
        }

        var productFeed = await adtractionRepository.GetProductFeed(programId.Value, feedId.Value);
        if (productFeed == null)
        {
            // TODO: refresh when not found
            return new();
        }

        var feedProducts = await adtractionRepository.GetFeedProducts(productFeed, skip, limit, searchQuery);
        return feedProducts;
    }

    public async Task<List<AdtractionProgram>> GetPrograms(string market, int? programId, int? channelId, int? approvalStatus, int? status)
    {
        try
        {
            var input = new
            {
                market,
                programId,
                channelId,
                approvalStatus,
                status
            };
            var inputJson = JsonConvert.SerializeObject(input);
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{url}/v3/partner/programs/?token={apiKey}"),
                Method = HttpMethod.Post,
                Content = new StringContent(inputJson, new MediaTypeHeaderValue("application/json"))
            };
            var resp = await httpClient.SendAsync(request);
            var respJson = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                var errorResp = JsonConvert.DeserializeObject<AdtractionError>(respJson);
                throw new Exception(errorResp?.Message ?? "Unknown adtraction error");
            }
            var parsedResp = JsonConvert.DeserializeObject<List<AdtractionProgram>>(respJson);
            if (parsedResp == null) throw new Exception("Failed to deserialize programs response");

            foreach (var program in parsedResp)
            {
                foreach (var feed in program.Feeds ?? new())
                {
                    feed.ProgramId = program.ProgramId;
                }
            }
            return parsedResp;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get programs");
            throw;
        }
    }
}