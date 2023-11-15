using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Ratings.Common;
using RecipesAPI.API.Features.Ratings.DAL;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Ratings.BLL;

public class RatingsService(RatingsRepository ratingsRepository, ILogger<RatingsService> logger, ICacheProvider cache)
{
    private readonly ILogger<RatingsService> logger = logger;
    private readonly RatingsRepository ratingsRepository = ratingsRepository;
    private readonly ICacheProvider cache = cache;

    private string RatingCacheKey(RatingType type, string entityId) => $"GetRating:{type}:{entityId}";
    private string ReactionsCacheKey(RatingType type, string entityId) => $"GetReactions:{type}:{entityId}";

    #region reactions
    public async Task<List<Reaction>> GetReactions(RatingType type, string id, CancellationToken cancellationToken)
    {
        var reactions = await ratingsRepository.GetReactions(type, id, cancellationToken);
        return reactions;
    }
    public async Task<Dictionary<string, List<Reaction>>> GetReactions(RatingType type, List<string> ids, CancellationToken cancellationToken)
    {
        var mutableIds = ids.ToList();
        var result = new Dictionary<string, List<Reaction>>();
        var fromCache = await cache.Get<List<Reaction>>(ids.Select(id => ReactionsCacheKey(type, id)).ToList(), cancellationToken);
        foreach (var reactions in fromCache)
        {
            if (reactions != null)
            {
                foreach (var reaction in reactions)
                {
                    if (result.ContainsKey(reaction.EntityId))
                    {
                        result[reaction.EntityId].Add(reaction);
                    }
                    else
                    {
                        result[reaction.EntityId] = [reaction];
                    }
                    mutableIds.Remove(reaction.EntityId);
                }
            }
        }

        var fromDb = await ratingsRepository.GetReactions(type, mutableIds, cancellationToken);
        foreach (var reaction in fromDb)
        {
            await cache.Put(ReactionsCacheKey(type, reaction.Key), reaction.Value, cancellationToken: cancellationToken);
            result[reaction.Key] = reaction.Value;
        }
        return await ratingsRepository.GetReactions(type, ids, cancellationToken);
    }

    public async Task<Reaction?> GetReaction(ReactionType reactionType, RatingType type, string id, string userId, CancellationToken cancellationToken)
    {
        var reaction = await ratingsRepository.GetReaction(reactionType, type, id, userId, cancellationToken);
        return reaction;
    }

    public async Task<Reaction?> GetReaction(string id, CancellationToken cancellationToken)
    {
        return await ratingsRepository.GetReaction(id, cancellationToken);
    }

    public async Task<Reaction> SaveReaction(Reaction reaction, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(reaction.Id))
        {
            reaction.Id = Guid.NewGuid().ToString();
        }
        try
        {
            await ratingsRepository.SaveReaction(reaction, cancellationToken);
            await ClearReactionCache(reaction.EntityType, reaction.EntityId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to save reaction with id {id}", reaction.Id);
            throw new GraphQLErrorException("failed to save reaction", ex);
        }

        var savedReaction = await ratingsRepository.GetReaction(reaction.Id, cancellationToken);
        if (savedReaction == null)
        {
            throw new GraphQLErrorException("could not get saved reaction");
        }
        return savedReaction;
    }

    public async Task DeleteReaction(Reaction reaction, CancellationToken cancellationToken)
    {
        try
        {
            await ratingsRepository.DeleteReaction(reaction, cancellationToken);
            await ClearReactionCache(reaction.EntityType, reaction.EntityId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to delete reaction with id {id}", reaction.Id);
            throw new GraphQLErrorException("faild to delete reaction", ex);
        }
    }

    private async Task ClearReactionCache(RatingType type, string entityId)
    {
        await cache.Remove(ReactionsCacheKey(type, entityId));
    }
    #endregion

    #region ratings
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
            throw new GraphQLErrorException("Failed to delete rating", ex);
        }
    }

    private async Task ClearCache(RatingType type, string entityId)
    {
        await cache.Remove(RatingCacheKey(type, entityId));
    }
    #endregion
}