using System.Xml.Serialization;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Admin.DAL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Admin.BLL;

public class PartnerAdsService(string url, string key, HttpClient httpClient, ILogger<PartnerAdsService> logger, PartnerAdsRepository partnerAdsRepository, ICacheProvider cache)
{
    private readonly ILogger<PartnerAdsService> logger = logger;
    private readonly string url = url;
    private readonly string key = key;
    private readonly HttpClient httpClient = httpClient;
    private readonly PartnerAdsRepository partnerAdsRepository = partnerAdsRepository;
    private readonly ICacheProvider cache = cache;

    private const string dateFormat = "yy-M-d";

    public async Task<List<(string programId, string categoryName)>> GetCategories()
    {
        return await partnerAdsRepository.GetCategories();
    }

    public async Task<PartnerAdsFeedProduct?> GetFeedProduct(PartnerAdsItemReference itemReference)
    {
        return await partnerAdsRepository.GetFeedProduct(itemReference.ProgramId, itemReference.ProductId);
    }

    public async Task<List<PartnerAdsFeedProduct>> GetFeedProducts(List<PartnerAdsItemReference> itemReferences)
    {
        var itemRefsTuple = itemReferences.Select(itemref => (itemref.ProgramId, itemref.ProductId)).ToList();
        return await partnerAdsRepository.GetFeedProducts(itemRefsTuple);
    }

    public async Task<PartnerAdsBalance> GetBalance()
    {
        try
        {
            var resp = await httpClient.GetStreamAsync($"{url}/saldo_xml.php?key={key}");
            var serializer = new XmlSerializer(typeof(PartnerAdsBalance));
            var balance = serializer.Deserialize(resp) as PartnerAdsBalance;
            return balance ?? new PartnerAdsBalance();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get balance");
            throw;
        }
    }

    public async Task<List<PartnerAdsProgram>> GetPrograms(bool publicView)
    {
        try
        {
            var cacheKey = "PartnerAds:GetPrograms";
            var cached = await cache.Get<List<PartnerAdsProgram>>(cacheKey);
            if (cached == null)
            {
                var resp = await httpClient.GetStreamAsync($"{url}/programoversigt_xml.php?key={key}&godkendte=1");
                var serializer = new XmlSerializer(typeof(PartnerAdsPrograms));
                cached = (serializer.Deserialize(resp) as PartnerAdsPrograms)?.Programs ?? [];
                await cache.Put(cacheKey, cached, expiration: TimeSpan.FromMinutes(10));
            }
            if (publicView)
            {
                foreach (var program in cached)
                {
                    program.ProgramDescription = "";
                    program.CategoryId = "";
                    program.CategoryName = "";
                    program.SubCategory = "";
                    program.ClickRate = 0;
                    program.LeadRate = 0;
                    program.Provision = 0;
                    program.Epc = null;
                    program.SemPpc = null;
                    program.SemPpcRestriction = null;
                    program.ShoppingAds = null;
                    program.ShoppingAdsRestriction = null;
                    program.SocialPpc = null;
                    program.Cashback = null;
                    program.Rabatsites = null;
                    program.ContactPerson = null;
                    program.Email = null;
                    program.Status = null;
                    program.Currency = "";
                    program.Market = "";
                    program.FeedUpdatedStr = null;
                }
            }
            return cached;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get partner programs");
            throw;
        }
    }

    public async Task<PartnerAdsEarning> GetEarning(DateOnly from, DateOnly to)
    {
        try
        {
            var fromStr = from.ToString(dateFormat);
            var toStr = to.ToString(dateFormat);
            var fullUrl = $"{url}/partnerindtjening_dato_xml.php?key={key}&fra={fromStr}&til={toStr}";
            var resp = await httpClient.GetStreamAsync(fullUrl);
            var serializer = new XmlSerializer(typeof(PartnerAdsEarningsInterval));
            var programs = serializer.Deserialize(resp) as PartnerAdsEarningsInterval;
            return programs?.Earning ?? new PartnerAdsEarning();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get earnings");
            throw;
        }
    }

    public async Task<PartnerAdsProgramStats> GetProgramStats(DateOnly from, DateOnly to)
    {
        try
        {
            var fromStr = from.ToString(dateFormat);
            var toStr = to.ToString(dateFormat);
            var fullUrl = $"{url}/programstat_xml.php?key={key}&fra={fromStr}&til={toStr}";
            var resp = await httpClient.GetStreamAsync(fullUrl);
            var serializer = new XmlSerializer(typeof(PartnerAdsProgramStats));
            var programStats = serializer.Deserialize(resp) as PartnerAdsProgramStats;
            return programStats ?? new PartnerAdsProgramStats();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get program stats");
            throw;
        }
    }

    public async Task<PartnerAdsClickSummary> GetClickSummary()
    {
        try
        {
            var fullUrl = $"{url}/klikoversigt_xml.php?key={key}";
            var resp = await httpClient.GetStreamAsync(fullUrl);
            var serializer = new XmlSerializer(typeof(PartnerAdsClickSummary));
            var programStats = serializer.Deserialize(resp) as PartnerAdsClickSummary;
            return programStats ?? new PartnerAdsClickSummary();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get click summary");
            throw;
        }
    }

    public async Task<List<PartnerAdsFeedProduct>> ParseProductFeed(string feedLink)
    {
        var xmlStream = await httpClient.GetStreamAsync(feedLink);
        var xmlSerializer = new XmlSerializer(typeof(PartnerAdsProductFeed));
        var productFeed = (xmlSerializer.Deserialize(xmlStream) as PartnerAdsProductFeed ?? new()).Products ?? new();
        return productFeed;
    }

    public async Task RefreshProductFeeds(string? programId, string? feedLink)
    {
        var programs = await GetPrograms(publicView: false);
        if (!string.IsNullOrEmpty(programId) && !string.IsNullOrEmpty(feedLink))
        {
            programs = programs.Where(x => x.ProgramId == programId && x.FeedLink == feedLink).ToList();
        }
        foreach (var program in programs)
        {
            if (program.Feed && !string.IsNullOrWhiteSpace(program.FeedLink) && program.FeedUpdated.HasValue)
            {
                var feedDto = await partnerAdsRepository.GetProductFeed(program.ProgramId, program.FeedLink);
                if (feedDto == null || feedDto.FeedUpdated < program.FeedUpdated.Value)
                {
                    var productFeed = await ParseProductFeed(program.FeedLink);
                    await partnerAdsRepository.SaveProductFeed(program.ProgramId, program.FeedLink, program.FeedUpdated.Value, productFeed);

                }
            }
        }
    }

    public async Task<List<PartnerAdsFeedProduct>> GetFeedProducts(string? programId, string? feedLink, int? skip, int? limit, string? searchQuery, bool retry = true)
    {
        if (string.IsNullOrEmpty(programId) || string.IsNullOrEmpty(feedLink))
        {
            return new();
        }
        var feedDto = await partnerAdsRepository.GetProductFeed(programId, feedLink);
        if (feedDto == null)
        {
            if (retry)
            {
                await RefreshProductFeeds(programId, feedLink);
                return await GetFeedProducts(programId, feedLink, skip, limit, searchQuery, retry: false);
            }
            else
            {
                return new();
            }
        }

        var feedProducts = await partnerAdsRepository.GetFeedProducts(feedDto, skip, limit, searchQuery);
        return feedProducts;
    }

    public async Task<List<PartnerAdsFeedProduct>> SearchFeedProducts(string? searchQuery, string? programId, int skip, int limit)
    {
        return await partnerAdsRepository.SearchFeedProducts(searchQuery, programId, skip, limit);
    }
}