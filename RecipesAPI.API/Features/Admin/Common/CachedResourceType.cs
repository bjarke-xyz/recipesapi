namespace RecipesAPI.API.Features.Admin.Common;

public class CachedResourceType
{
    public CachedResourceType(string name)
    {
        Name = name;
    }
    public CachedResourceType() { }

    public string Name { get; set; } = default!;
}

public class CachedResourceTypeHelper
{
    public static readonly CachedResourceType RECIPES = new CachedResourceType("RECIPES");
    public static readonly CachedResourceType USERS = new CachedResourceType("USERS");
    public static readonly CachedResourceType FILES = new CachedResourceType("FILES");
    public static readonly CachedResourceType EQUIPMENT = new CachedResourceType("EQUIPMENT");
}