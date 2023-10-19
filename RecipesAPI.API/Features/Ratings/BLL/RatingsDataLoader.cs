using RecipesAPI.API.Features.Ratings.Common;

namespace RecipesAPI.API.Features.Ratings.BLL;

public class RecipeRatingsDataLoader : BatchDataLoader<string, List<Rating>>
{
    private readonly RatingsService ratingsService;

    public RecipeRatingsDataLoader(RatingsService ratingsService, IBatchScheduler batchScheduler, DataLoaderOptions? options = null) : base(batchScheduler, options)
    {
        this.ratingsService = ratingsService;
    }

    protected override async Task<IReadOnlyDictionary<string, List<Rating>>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        var ratings = await ratingsService.GetRatings(RatingType.Recipe, keys.ToList(), cancellationToken);
        return ratings;
    }
}