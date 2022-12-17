using RecipesAPI.API.Auth;
using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Features.Admin.Graph;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class AdminMutations
{
    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<bool> ClearCache(List<CachedResourceType> cachedResourceTypes, [Service] AdminService adminService, CancellationToken cancellationToken)
    {
        await adminService.ClearCache(cachedResourceTypes, cancellationToken);
        return true;
    }
}