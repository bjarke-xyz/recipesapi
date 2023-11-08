using System.Net.Http.Headers;
using System.Xml.Serialization;
using Newtonsoft.Json;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Admin.Common.Adtraction;
using RecipesAPI.API.Features.Admin.DAL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Admin.BLL;

public class AdtractionService(ILogger<AdtractionService> logger, string url, string apiKey, HttpClient httpClient, AdtractionRepository adtractionRepository, string defaultMarket, int defaultChannelId, ICacheProvider cache)
{
    private readonly ILogger<AdtractionService> logger = logger;
    private readonly string url = url;
    private readonly string apiKey = apiKey;
    private readonly HttpClient httpClient = httpClient;
    private readonly AdtractionRepository adtractionRepository = adtractionRepository;
    private readonly string defaultMarket = defaultMarket;
    private readonly int defaultChannelId = defaultChannelId;
    private readonly ICacheProvider cache = cache;

    public async Task<AdtractionFeedProduct?> GetFeedProduct(AdtractionItemReference itemReference)
    {
        return await adtractionRepository.GetFeedProduct(itemReference.ProgramId, itemReference.FeedId, itemReference.Sku);
    }

    public async Task<List<AdtractionFeedProduct>> SearchFeedProducts(string? searchQuery, string? programId, int skip, int limit)
    {
        int? programIdInt = null;
        if (int.TryParse(programId, out var _programIdInt))
        {
            programIdInt = _programIdInt;
        }
        return await adtractionRepository.SearchFeedProducts(searchQuery, programIdInt, skip, limit);
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

    public async Task RefreshProductFeeds(string market, int channelId, int? programId, int? feedId)
    {
        var programs = await GetPrograms(market, programId, channelId, 1, null);
        foreach (var program in programs)
        {
            foreach (var feed in program.Feeds ?? new())
            {
                if (feedId.HasValue && feed.FeedId != feedId)
                {
                    continue;
                }
                if (!feed.FeedId.HasValue || !feed.LastUpdated.HasValue || string.IsNullOrWhiteSpace(feed.FeedUrl))
                {
                    continue;
                }
                var feedDto = await adtractionRepository.GetProductFeed(program.ProgramId, feed.FeedId.Value);
                if (feedDto == null || feedDto.LastUpdated < feed.LastUpdated)
                {
                    var productFeed = await ParseProductFeed(feed.FeedUrl);
                    await adtractionRepository.SaveProductFeed(program.ProgramId, feed, productFeed);
                }

            }
        }
    }

    public async Task<List<AdtractionFeedProduct>> GetFeedProducts(int? programId, int? feedId, int? skip, int? limit, string? searchQuery, bool retry = true)
    {
        if (!programId.HasValue || !feedId.HasValue)
        {
            return new();
        }

        var productFeed = await adtractionRepository.GetProductFeed(programId.Value, feedId.Value);
        if (productFeed == null)
        {
            if (retry)
            {
                await RefreshProductFeeds(defaultMarket, defaultChannelId, programId, feedId);
                return await GetFeedProducts(programId, feedId, skip, limit, searchQuery, retry: false);
            }
            else
            {
                return new();
            }
        }

        var feedProducts = await adtractionRepository.GetFeedProducts(productFeed, skip, limit, searchQuery);
        return feedProducts;
    }

    public async Task<List<AdtractionProgram>> GetPrograms(string market, int? programId, int? channelId, int? approvalStatus, int? status)
    {
        try
        {
            var cacheKey = $"Adtraction:GetPrograms:{market}:{programId}:{channelId}:{approvalStatus}:{status}";
            var cached = await cache.Get<List<AdtractionProgram>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
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
            await cache.Put(cacheKey, parsedResp, expiration: TimeSpan.FromMinutes(10));
            return parsedResp;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get programs");
            throw;
        }
    }
}