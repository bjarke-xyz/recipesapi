
using RecipesAPI.API.Features.Ratings.Common;

namespace RecipesAPI.API.Features.Ratings.BLL;

public class RecipeReactionsDataLoader : BatchDataLoader<string, List<Reaction>>
{
    private readonly RatingsService ratingsService;

    public RecipeReactionsDataLoader(RatingsService ratingsService, IBatchScheduler batchScheduler, DataLoaderOptions? options = null) : base(batchScheduler, options)
    {
        this.ratingsService = ratingsService;
    }

    protected override async Task<IReadOnlyDictionary<string, List<Reaction>>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        var reactions = await ratingsService.GetReactions(RatingType.Recipe, keys.ToList(), cancellationToken);
        return reactions;
    }
}