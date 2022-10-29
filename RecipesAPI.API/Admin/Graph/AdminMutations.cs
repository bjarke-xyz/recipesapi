using RecipesAPI.Admin.BLL;
using RecipesAPI.Admin.Common;
using RecipesAPI.Auth;
using RecipesAPI.Users.Common;

namespace RecipesAPI.Admin.Graph;

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