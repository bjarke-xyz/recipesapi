using RecipesAPI.API.Admin.BLL;
using RecipesAPI.API.Admin.Common;
using RecipesAPI.API.Auth;
using RecipesAPI.API.Users.Common;

namespace RecipesAPI.API.Admin.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class AdminQueries
{
    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public IEnumerable<CachedResourceType> GetCachedResourceTypes([Service] AdminService adminService, CancellationToken cancellationToken)
    {
        return adminService.GetCachedResourceTypes();
    }
}