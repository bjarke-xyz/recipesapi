using RecipesAPI.Admin.BLL;
using RecipesAPI.Admin.Common;
using RecipesAPI.Auth;
using RecipesAPI.Users.Common;

namespace RecipesAPI.Admin.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class AdminQueries
{
    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public IEnumerable<CachedResourceType> GetCachedResourceTypes([Service] AdminService adminService, CancellationToken cancellationToken)
    {
        return adminService.GetCachedResourceTypes();
    }
}