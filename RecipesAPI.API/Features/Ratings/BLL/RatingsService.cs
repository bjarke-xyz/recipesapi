using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Ratings.Common;
using RecipesAPI.API.Features.Ratings.DAL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Ratings.BLL;

public class RatingsService
{
    private readonly ILogger<RatingsService> logger;
    private readonly RatingsRepository ratingsRepository;
    private readonly ICacheProvider cache;

    private string RatingCacheKey(RatingType type, string entityId) => $"GetRating:{type}:{entityId}";

    public RatingsService(RatingsRepository ratingsRepository, ILogger<RatingsService> logger, ICacheProvider cache)
    {
        this.ratingsRepository = ratingsRepository;
        this.logger = logger;
        this.cache = cache;
    }

    public async Task<List<Rating>> GetRatings(RatingType type, string id, CancellationToken cancellationToken)
    {
        var ratings = await ratingsRepository.GetRatings(type, id, cancellationToken);
        return ratings;
    }

    public async Task<Dictionary<string, List<Rating>>> GetRatings(RatingType type, List<string> ids, CancellationToken cancellationToken)
    {
        var mutableIds = ids.ToList();
        var result = new Dictionary<string, List<Rating>>();
        var fromCache = await cache.Get<List<Rating>>(ids.Select(id => RatingCacheKey(type, id)).ToList(), cancellationToken);
        foreach (var ratings in fromCache)
        {
            if (ratings != null)
            {
                foreach (var rating in ratings)
                {
                    if (result.ContainsKey(rating.EntityId))
                    {
                        result[rating.EntityId].Add(rating);
                    }
                    else
                    {
                        result[rating.EntityId] = [rating];
                    }
                    mutableIds.Remove(rating.EntityId);
                }
            }
        }

        var fromDb = await ratingsRepository.GetRatings(type, mutableIds, cancellationToken);
        foreach (var rating in fromDb)
        {
            await cache.Put(RatingCacheKey(type, rating.Key), rating.Value, cancellationToken: cancellationToken);
            result[rating.Key] = rating.Value;
        }

        return await ratingsRepository.GetRatings(type, ids, cancellationToken);
    }

    public async Task<Rating?> GetRating(RatingType type, string id, string userId, CancellationToken cancellationToken)
    {
        var rating = await ratingsRepository.GetRating(type, id, userId, cancellationToken);
        return rating;
    }

    public async Task<Rating?> GetRating(string id, CancellationToken cancellationToken)
    {
        return await ratingsRepository.GetRating(id, cancellationToken);
    }

    public async Task<Rating> SaveRating(Rating rating, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rating.Id))
        {
            rating.Id = Guid.NewGuid().ToString();
        }
        if (rating.Score < RatingHelper.MinRating || rating.Score > RatingHelper.MaxRating)
        {
            throw new GraphQLErrorException($"Score must be between {RatingHelper.MinRating} and {RatingHelper.MaxRating}");
        }
        try
        {
            await ratingsRepository.SaveRating(rating, cancellationToken);
            await ClearCache(rating.EntityType, rating.EntityId);
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
            await ClearCache(rating.EntityType, rating.EntityId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete rating with id {id}", rating.Id);
            throw new GraphQLErrorException("Failed to delete rating");
        }
    }

    private async Task ClearCache(RatingType type, string entityId)
    {
        await cache.Remove(RatingCacheKey(type, entityId));
    }
}