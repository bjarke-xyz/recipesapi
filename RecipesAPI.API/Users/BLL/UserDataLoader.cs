using RecipesAPI.Users.Common;

namespace RecipesAPI.Users.BLL;

public class UserDataLoader : BatchDataLoader<string, User>
{
    private readonly UserService userService;

    public UserDataLoader(UserService userService, IBatchScheduler batchScheduler, DataLoaderOptions? options = null) : base(batchScheduler, options)
    {
        this.userService = userService;
    }

    protected override async Task<IReadOnlyDictionary<string, User>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        var users = await userService.GetUsersByIds(keys, cancellationToken);
        return users;
    }
}