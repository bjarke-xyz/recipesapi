using System.Xml.Serialization;
using RecipesAPI.API.Features.Admin.Common;

namespace RecipesAPI.API.Features.Admin.BLL;

public class PartnerAdsService
{
    private readonly ILogger<PartnerAdsService> logger;
    private readonly string url;
    private readonly string key;
    private readonly HttpClient httpClient;

    private const string dateFormat = "yy-M-d";

    public PartnerAdsService(string url, string key, HttpClient httpClient, ILogger<PartnerAdsService> logger)
    {
        this.url = url;
        this.key = key;
        this.httpClient = httpClient;
        this.logger = logger;
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


}