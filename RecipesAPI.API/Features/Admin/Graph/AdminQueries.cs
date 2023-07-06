using RecipesAPI.API.Auth;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Admin.Common.Adtraction;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Features.Admin.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class AdminQueries
{
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
            var partnerPrograms = await partnerAdsService.GetPrograms();
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

    public async Task<List<AdtractionProgram>> GetAdtractionPrograms([Service] AdtractionService adtractionService, AdtractionProgramsInput input)
    {
        try
        {
            return await adtractionService.GetPrograms(input.Market, input.ProgramId, input.ChannelId, input.ApprovalStatus, input.Status);
        }
        catch (Exception ex)
        {
            throw new GraphQLErrorException(ex.Message, ex);
        }
    }
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
    public string Market { get; set; }

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