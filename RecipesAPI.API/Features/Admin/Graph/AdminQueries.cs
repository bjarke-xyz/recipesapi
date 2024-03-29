using RecipesAPI.API.Auth;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Admin.Common.Adtraction;
using RecipesAPI.API.Features.Admin.DAL;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Features.Admin.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class AdminQueries
{
    public async Task<List<PartnerCategories>> GetPartnerCategories([Service] AdtractionService adtractionService, [Service] PartnerAdsService partnerAdsService)
    {
        var adtractionCategoriesList = await adtractionService.GetCategories();
        var adtractionCategories = adtractionCategoriesList.GroupBy(x => x.programId).ToDictionary(x => x.Key, x => x.ToList());
        var partnerAdsCategoriesList = await partnerAdsService.GetCategories();
        var partnerAdsCategories = partnerAdsCategoriesList.GroupBy(x => x.programId).ToDictionary(x => x.Key, x => x.ToList());

        var partnerCategories = new List<PartnerCategories>();
        foreach (var (programId, categories) in adtractionCategories)
        {
            partnerCategories.Add(new PartnerCategories
            {
                Provider = AffiliateProvider.Adtraction,
                ProgramId = programId,
                Categories = categories.Select(x => x.category).ToList(),
            });
        }
        foreach (var (programId, categories) in partnerAdsCategories)
        {
            partnerCategories.Add(new PartnerCategories
            {
                Provider = AffiliateProvider.PartnerAds,
                ProgramId = programId,
                Categories = categories.Select(x => x.categoryName).ToList(),
            });
        }
        return partnerCategories;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<SettingsDto> GetSettings([Service] SettingsService settingsService)
    {
        return await settingsService.GetSettings();
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public IEnumerable<CachedResourceType> GetCachedResourceTypes([Service] AdminService adminService, CancellationToken cancellationToken)
    {
        return adminService.GetCachedResourceTypes();
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<List<PartnerAdsProgram>> GetPartnerAdsPrograms([Service] PartnerAdsService partnerAdsService, CancellationToken cancellationToken)
    {
        try
        {
            var partnerPrograms = await partnerAdsService.GetPrograms(publicView: false);
            return partnerPrograms;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    /// <summary>
    /// The following properties have values: programId, programName, feedLink
    /// </summary>
    /// <param name="partnerAdsService"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="GraphQLErrorException"></exception>
    public async Task<List<PartnerAdsProgram>> GetPublicPartnerAdsPrograms([Service] PartnerAdsService partnerAdsService, CancellationToken cancellationToken)
    {
        try
        {
            var partnerPrograms = await partnerAdsService.GetPrograms(publicView: true);
            return partnerPrograms;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<PartnerAdsBalance> GetPartnerAdsBalance([Service] PartnerAdsService partnerAdsService, CancellationToken cancellationToken)
    {
        try
        {
            var balance = await partnerAdsService.GetBalance();
            return balance;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<PartnerAdsEarning> GetPartnerAdsEarning(PartnerAdsEarningsInput input, [Service] PartnerAdsService partnerAdsService, CancellationToken cancellationToken)
    {
        try
        {
            var earnings = await partnerAdsService.GetEarning(input.From, input.To);
            return earnings;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<PartnerAdsProgramStats> GetPartnerAdsProgramStats(PartnerAdsProgramStatsInput input, [Service] PartnerAdsService partnerAdsService, CancellationToken cancellationToken)
    {
        try
        {
            var programStats = await partnerAdsService.GetProgramStats(input.From, input.To);
            return programStats;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<PartnerAdsClickSummary> GetPartnerAdsClickSummary([Service] PartnerAdsService partnerAdsService, CancellationToken cancellationToken)
    {
        try
        {
            var clickSummary = await partnerAdsService.GetClickSummary();
            return clickSummary;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<AdtractionAccountBalance> GetAdtractionAccountBalance([Service] AdtractionService adtractionService, string currency = "DKK")
    {
        try
        {
            var balance = await adtractionService.GetBalance(currency);
            return balance;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<List<AdtractionApplication>> GetAdtractionApplications([Service] AdtractionService adtractionService, [IdToken] string idToken)
    {
        try
        {
            return await adtractionService.GetApplications();
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<List<AdtractionProgram>> GetAdtractionPrograms([Service] AdtractionService adtractionService, AdtractionProgramsInput input)
    {
        try
        {
            return await adtractionService.GetPrograms(input.Market, input.ProgramId, input.ChannelId, input.ApprovalStatus ?? 1, input.Status ?? 0);
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    public async Task<List<PublicAdtractionProgram>> GetPublicAdtractionPrograms([Service] AdtractionService adtractionService, AdtractionProgramsInput input)
    {
        try
        {
            var adtractionPrograms = await adtractionService.GetPrograms(input.Market, input.ProgramId, input.ChannelId, input.ApprovalStatus ?? 1, input.Status ?? 0);
            var publicAdtractionPrograms = AdminMapper.Map(adtractionPrograms);
            return publicAdtractionPrograms;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    public async Task<List<AffiliateItem>> SearchAffiliateItems(SearchProductFeedInput input, [Service] AffiliateService affiliateService)
    {
        try
        {
            return await affiliateService.SearchAffiliateItems(input.SearchQuery, input.Count ?? 1000);
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }
}

[ExtendObjectType(typeof(Feed))]
public class AdtractionFeedQueries
{
    [Obsolete("Use AffiliateItems")]
    public async Task<List<AdtractionFeedProduct>> GetProductFeed([Parent] Feed feed, [Service] AdtractionService adtractionService, GetProductFeedInput? input = null)
    {
        try
        {
            if (string.IsNullOrEmpty(feed.FeedUrl))
            {
                return new();
            }
            var feedProducts = await adtractionService.GetFeedProducts(feed.ProgramId, feed.FeedId, input?.Skip ?? 0, input?.Limit ?? 1000, input?.SearchQuery);
            return feedProducts;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    public async Task<List<AffiliateItem>> GetAffiliateItems([Parent] Feed feed, [Service] AdtractionService adtractionService, GetProductFeedInput? input = null)
    {
        try
        {
            if (string.IsNullOrEmpty(feed.FeedUrl))
            {
                return new();
            }
            var feedProducts = await adtractionService.GetFeedProducts(feed.ProgramId, feed.FeedId, input?.Skip ?? 0, input?.Limit ?? 1000, input?.SearchQuery);
            var affiliateItems = feedProducts.Select(x => new AffiliateItem(x)).Where(x => x.ItemInfo != null).ToList();
            return affiliateItems;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }
}

[ExtendObjectType(typeof(PartnerAdsProgram))]
public class PartnerAdsProgramQueries
{
    [Obsolete("Use AffiliateItems")]
    public async Task<List<PartnerAdsFeedProduct>> GetProductFeed([Parent] PartnerAdsProgram program, [Service] PartnerAdsService partnerAdsService, GetProductFeedInput? input = null)
    {
        try
        {
            if (string.IsNullOrEmpty(program.FeedLink))
            {
                return new();
            }
            var feedProducts = await partnerAdsService.GetFeedProducts(program.ProgramId, program.FeedLink, input?.Skip ?? 0, input?.Limit ?? 1000, input?.SearchQuery);
            return feedProducts;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }

    public async Task<List<AffiliateItem>> GetAffiliateItems([Parent] PartnerAdsProgram program, [Service] PartnerAdsService partnerAdsService, GetProductFeedInput? input = null)
    {
        try
        {
            if (string.IsNullOrEmpty(program.FeedLink))
            {
                return new();
            }
            var feedProducts = await partnerAdsService.GetFeedProducts(program.ProgramId, program.FeedLink, input?.Skip ?? 0, input?.Limit ?? 1000, input?.SearchQuery);
            var affiliateItems = feedProducts.Select(x => new AffiliateItem(x)).Where(x => x.ItemInfo != null).ToList();
            return affiliateItems;
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }
}

[ExtendObjectType(typeof(AffiliateItemReference))]
public class AffiliateItemReferenceQueries
{
    public async Task<AffiliateItem?> GetAffiliateItem([Parent] AffiliateItemReference? affiliateItemReference, [Service] AffiliateService affiliateService)
    {
        if (affiliateItemReference == null) return null;
        var affiliateItem = await affiliateService.GetAffiliateItem(affiliateItemReference);
        return affiliateItem;
    }
}

public class GetProductFeedInput
{
    public int? Skip { get; set; }
    public int? Limit { get; set; }
    public string? SearchQuery { get; set; }
}

public class SearchProductFeedInput
{
    public string? SearchQuery { get; set; }
    public int? Count { get; set; }
}

public class PartnerAdsEarningsInput
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}

public class PartnerAdsProgramStatsInput
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}

public class AdtractionProgramsInput
{

    /// <summary>
    /// Geographical market on which a partner program is available, defined by an ISO 3166-1 Alpha-2 country code
    /// </summary>
    public string Market { get; set; } = "";

    /// <summary>
    /// Numerical ID of an partner program
    /// </summary>
    public int? ProgramId { get; set; }

    /// <summary>
    /// Numerical ID of a channel
    /// </summary>
    public int? ChannelId { get; set; }

    /// <summary>
    /// Approval status for a partner program: 0 = rejected, 1 = approved, 2 = pending review
    /// </summary>
    public int? ApprovalStatus { get; set; }

    /// <summary>
    /// The status of the partner program on the Adtraction platform, where Live = 0 and Closing = 3
    /// </summary>
    public int? Status { get; set; }
}

public class PartnerCategories
{
    public AffiliateProvider Provider { get; set; }
    public string ProgramId { get; set; } = "";
    public List<string> Categories { get; set; } = [];
}