using System.Xml.Serialization;
using Amazon.S3.Model.Internal.MarshallTransformations;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Admin.DAL;

namespace RecipesAPI.API.Features.Admin.BLL;

public class PartnerAdsService
{
    private readonly ILogger<PartnerAdsService> logger;
    private readonly string url;
    private readonly string key;
    private readonly HttpClient httpClient;
    private readonly PartnerAdsRepository partnerAdsRepository;

    private const string dateFormat = "yy-M-d";

    public PartnerAdsService(string url, string key, HttpClient httpClient, ILogger<PartnerAdsService> logger, PartnerAdsRepository partnerAdsRepository)
    {
        this.url = url;
        this.key = key;
        this.httpClient = httpClient;
        this.logger = logger;
        this.partnerAdsRepository = partnerAdsRepository;
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

    public async Task<List<PartnerAdsProgram>> GetPrograms()
    {
        try
        {
            var resp = await httpClient.GetStreamAsync($"{url}/programoversigt_xml.php?key={key}&godkendte=1");
            var serializer = new XmlSerializer(typeof(PartnerAdsPrograms));
            var programs = serializer.Deserialize(resp) as PartnerAdsPrograms;
            return programs?.Programs ?? new List<PartnerAdsProgram>();
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

    public async Task RefreshProductFeeds()
    {
        var programs = await GetPrograms();
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
                await RefreshProductFeeds();
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


}