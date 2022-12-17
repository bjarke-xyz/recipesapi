using HotChocolate.AspNetCore.Authorization;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Auth;

public class RoleAuthorizeAttribute : AuthorizeAttribute
{
    private Role[]? _roleEnums;
    public Role[]? RoleEnums
    {
        get
        {
            return _roleEnums;
        }
        set
        {
            if (value != null)
            {
                this._roleEnums = value;
                this.Roles = value.Select(x => x.ToString()).ToArray();
            }
        }
    }
}