using RecipesAPI.API.Admin.BLL;
using RecipesAPI.API.Admin.Common;
using RecipesAPI.API.Auth;
using RecipesAPI.API.Users.Common;

namespace RecipesAPI.API.Admin.Graph;

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