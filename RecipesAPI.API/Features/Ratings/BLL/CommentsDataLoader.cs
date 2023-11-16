using RecipesAPI.API.Features.Ratings.Common;

namespace RecipesAPI.API.Features.Ratings.BLL;

public class RecipeCommentsDataLoader : BatchDataLoader<string, List<Comment>>
{
    private readonly RatingsService ratingsService;

    public RecipeCommentsDataLoader(RatingsService ratingsService, IBatchScheduler batchScheduler, DataLoaderOptions? options = null) : base(batchScheduler, options)
    {
        this.ratingsService = ratingsService;
    }

    protected override async Task<IReadOnlyDictionary<string, List<Comment>>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        var comments = await ratingsService.GetComments(RatingType.Recipe, keys.ToList(), cancellationToken);
        return comments;
    }
}