using HotChocolate.AspNetCore.Authorization;
using RecipesAPI.Users;
using RecipesAPI.Users.Common;

namespace RecipesAPI.Auth;

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