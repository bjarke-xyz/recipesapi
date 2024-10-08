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
    private string EntityIdFromRatingCacheKey(string cacheKey) => cacheKey.Split(":").Last();
    private string ReactionsCacheKey(RatingType type, string entityId) => $"GetReactions:{type}:{entityId}";
    private string EntityIdFromReactionCacheKey(string cacheKey) => cacheKey.Split(":").Last();
    private string CommentCacheKey(string id) => $"GetComment:v2:{id}";
    private string CommentsCacheKey(RatingType type, string entityId) => $"GetComments:v2:{type}:{entityId}";
    private string CommentsDictCacheKey(RatingType type, List<string> entityIds) => $"GetComments-dict:v2:{type}:{string.Join('-', entityIds)}";

    #region comments
    public async Task<List<Comment>> GetComments(RatingType type, string id, CancellationToken cancellationToken, bool buildTree = false)
    {
        var comments = await cache.Get<List<Comment>>(CommentsCacheKey(type, id), cancellationToken);
        if (comments == null)
        {
            comments = await ratingsRepository.GetComments(type, id, cancellationToken);
            await cache.Put(CommentsCacheKey(type, id), comments, cancellationToken: cancellationToken);
        }
        if (buildTree)
        {
            comments = CommentsUtil.BuildTree(comments);
        }
        return comments;
    }

    public async Task<Dictionary<string, List<Comment>>> GetComments(RatingType type, List<string> ids, CancellationToken cancellationToken, bool buildTree = false)
    {
        var commentsDict = await cache.Get<Dictionary<string, List<Comment>>>(CommentsDictCacheKey(type, ids), cancellationToken);
        if (commentsDict == null)
        {
            commentsDict = await ratingsRepository.GetComments(type, ids, cancellationToken);
            await cache.Put(CommentsDictCacheKey(type, ids), commentsDict, cancellationToken: cancellationToken);
        }
        var keys = commentsDict.Keys.ToList();
        foreach (var key in keys)
        {
            var comments = commentsDict[key];
            commentsDict[key] = buildTree ? CommentsUtil.BuildTree(comments) : comments;
        }
        return commentsDict;
    }

    public async Task<Comment?> GetComment(string id, CancellationToken cancellationToken)
    {
        var comment = await cache.Get<Comment>(CommentCacheKey(id), cancellationToken);
        if (comment == null)
        {
            comment = await ratingsRepository.GetComment(id, cancellationToken);
            if (comment != null)
            {
                await cache.Put(CommentCacheKey(id), comment, cancellationToken: cancellationToken);
            }
        }
        return comment;
    }

    public async Task<Comment> SaveComment(Comment comment, CancellationToken cancellationToken, bool create)
    {
        if (string.IsNullOrWhiteSpace(comment.Message))
        {
            throw new GraphQLErrorException("invalid message");
        }

        if (!string.IsNullOrWhiteSpace(comment.ParentCommentId) && create)
        {
            var parentComment = await GetComment(comment.ParentCommentId, cancellationToken);
            if (parentComment == null)
            {
                throw new GraphQLErrorException("parent comment not found");
            }
            if (parentComment.DeletedAt.HasValue == true)
            {
                throw new GraphQLErrorException("cannot reply to deleted comment");
            }
        }
        if (string.IsNullOrEmpty(comment.Id))
        {
            comment.Id = Guid.NewGuid().ToString();
        }
        await ratingsRepository.SaveComment(comment, cancellationToken);
        await ClearCache(comment, cancellationToken);
        return await GetComment(comment.Id, cancellationToken) ?? throw new GraphQLErrorException("failed to get saved comment");
    }

    public async Task<Comment> DeleteComment(Comment comment, CancellationToken cancellationToken)
    {
        await ratingsRepository.DeleteComment(comment, cancellationToken);
        await ClearCache(comment, cancellationToken);
        return await GetComment(comment.Id, cancellationToken) ?? throw new GraphQLErrorException("failed to get soft deleted comment");
    }

    private async Task ClearCache(Comment comment, CancellationToken cancellationToken)
    {
        await cache.Remove(CommentCacheKey(comment.Id), cancellationToken);
        await cache.Remove(CommentsCacheKey(comment.EntityType, comment.EntityId), cancellationToken);
        await cache.RemoveByPrefix(CommentsDictCacheKey(comment.EntityType, []), cancellationToken);
    }

    #endregion

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
        foreach (var (key, reactions) in fromCache)
        {
            if (reactions != null)
            {
                var entityId = EntityIdFromReactionCacheKey(key);
                mutableIds.Remove(entityId);
                foreach (var reaction in reactions)
                {
                    if (result.TryGetValue(reaction.EntityId, out List<Reaction>? value))
                    {
                        value.Add(reaction);
                    }
                    else
                    {
                        result[reaction.EntityId] = [reaction];
                    }
                }
            }
        }

        var fromDb = await ratingsRepository.GetReactions(type, mutableIds, cancellationToken);
        foreach (var reaction in fromDb)
        {
            await cache.Put(ReactionsCacheKey(type, reaction.Key), reaction.Value, cancellationToken: cancellationToken);
            result[reaction.Key] = reaction.Value;
            mutableIds.Remove(reaction.Key);
        }

        // load cache with empty values for remaining ids, so we dont hit firestore directly every time for these
        foreach (var id in mutableIds)
        {
            await cache.Put(ReactionsCacheKey(type, id), new List<Reaction>(), cancellationToken: cancellationToken);
        }

        return result;
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
    public async Task<List<Rating>> GetAllRatings(CancellationToken cancellationToken)
    {
        return await ratingsRepository.GetAllRatings(cancellationToken);
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
        foreach (var (key, ratings) in fromCache)
        {
            if (ratings != null)
            {
                var entityId = EntityIdFromRatingCacheKey(key);
                mutableIds.Remove(entityId);
                foreach (var rating in ratings)
                {
                    if (result.TryGetValue(rating.EntityId, out List<Rating>? value))
                    {
                        value.Add(rating);
                    }
                    else
                    {
                        result[rating.EntityId] = [rating];
                    }
                }
            }
        }

        var fromDb = await ratingsRepository.GetRatings(type, mutableIds, cancellationToken);
        foreach (var rating in fromDb)
        {
            await cache.Put(RatingCacheKey(type, rating.Key), rating.Value, cancellationToken: cancellationToken);
            result[rating.Key] = rating.Value;
        }

        return result;
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