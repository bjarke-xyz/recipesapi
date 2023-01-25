using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Ratings.Common;
using RecipesAPI.API.Features.Ratings.DAL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Ratings.BLL;

// TODO: Caching?
public class RatingsService
{
    private readonly ILogger<RatingsService> logger;
    private readonly RatingsRepository ratingsRepository;

    public RatingsService(RatingsRepository ratingsRepository, ILogger<RatingsService> logger)
    {
        this.ratingsRepository = ratingsRepository;
        this.logger = logger;
    }

    public async Task<List<Rating>> GetRatings(RatingType type, CancellationToken cancellationToken)
    {
        var ratings = await ratingsRepository.GetRatings(type, cancellationToken);
        return ratings;
    }

    public async Task<List<Rating>> GetRatings(RatingType type, string id, CancellationToken cancellationToken)
    {
        var ratings = await ratingsRepository.GetRatings(type, id, cancellationToken);
        return ratings;
    }

    public async Task<Rating?> GetRating(RatingType type, string id, string userId, CancellationToken cancellationToken)
    {
        var rating = await ratingsRepository.GetRating(type, id, userId, cancellationToken);
        return rating;
    }

    public async Task<Rating> SaveRating(Rating rating, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rating.Id))
        {
            rating.Id = Guid.NewGuid().ToString();
        }
        if (rating.Score < 0 || rating.Score > 5)
        {
            throw new GraphQLErrorException("Score must be between 0 and 5");
        }
        try
        {
            await ratingsRepository.SaveRating(rating, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save rating with id {id}", rating.Id);
            throw new GraphQLErrorException("Failed to save rating", ex);
        }

        var savedRating = await ratingsRepository.GetRating(rating.Id, cancellationToken);
        if (savedRating == null)
        {
            throw new GraphQLErrorException("Could not get saved rating");
        }
        return savedRating;
    }

    public async Task DeleteRating(Rating rating, CancellationToken cancellationToken)
    {
        try
        {
            await ratingsRepository.DeleteRating(rating, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete rating with id {id}", rating.Id);
            throw new GraphQLErrorException("Failed to delete rating");
        }
    }
}