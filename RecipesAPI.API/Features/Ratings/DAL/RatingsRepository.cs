using Google.Cloud.Firestore;
using RecipesAPI.API.Features.Ratings.Common;

namespace RecipesAPI.API.Features.Ratings.DAL;

public class RatingsRepository(ILogger<RatingsRepository> logger, FirestoreDb db)
{
    private readonly ILogger<RatingsRepository> logger = logger;
    private readonly FirestoreDb db = db;
    private const string ratingCollection = "ratings";
    private const string reactionsCollection = "reactions";
    private const string commentsCollection = "comments";

    #region comments

    public async Task<List<Comment>> GetComments(RatingType type, string id, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(commentsCollection)
            .WhereEqualTo("entityType", type)
            .WhereEqualTo("entityId", id)
            .GetSnapshotAsync(cancellationToken);
        var comments = new List<Comment>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<CommentDto>();
            var comment = RatingMapper.MapDto(dto);
            comments.Add(comment);
        }
        // https://firebase.google.com/docs/firestore/manage-data/add-data#add_a_document
        // "If you want to be able to order your documents by creation date, you should store a timestamp as a field in the documents."
        return [.. comments.OrderBy(x => x.CreatedAt)];
    }

    public async Task<Dictionary<string, List<Comment>>> GetComments(RatingType type, List<string> ids, CancellationToken cancellationToken)
    {
        if (ids == null || ids.Count == 0)
        {
            return [];
        }
        var snapshot = await db.Collection(commentsCollection)
            .WhereEqualTo("entityType", type)
            .WhereIn("entityId", ids)
            .GetSnapshotAsync(cancellationToken);
        var comments = new List<Comment>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<CommentDto>();
            var comment = RatingMapper.MapDto(dto);
            comments.Add(comment);
        }
        var dict = comments.GroupBy(x => x.EntityId).ToDictionary(x => x.Key, x => x.ToList());
        // https://firebase.google.com/docs/firestore/manage-data/add-data#add_a_document
        // "If you want to be able to order your documents by creation date, you should store a timestamp as a field in the documents."
        foreach (var key in dict.Keys.ToList())
        {
            dict[key] = [.. dict[key].OrderBy(x => x.CreatedAt)];
        }
        return dict;
    }

    public async Task<Comment?> GetComment(string id, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(commentsCollection).Document(id).GetSnapshotAsync(cancellationToken);
        if (!snapshot.Exists) return null;
        var dto = snapshot.ConvertTo<CommentDto>();
        var comment = RatingMapper.MapDto(dto);
        return comment;
    }

    public async Task SaveComment(Comment comment, CancellationToken cancellationToken)
    {
        var dto = RatingMapper.Map(comment);
        await db.Collection(commentsCollection).Document(dto.Id).SetAsync(dto, null, cancellationToken);
    }

    public async Task DeleteComment(Comment comment, CancellationToken cancellationToken)
    {
        var dbComment = await GetComment(comment.Id, cancellationToken);
        if (dbComment == null) return;
        dbComment.DeletedAt = DateTimeOffset.UtcNow;
        dbComment.Message = "";
        await SaveComment(dbComment, cancellationToken);
    }

    #endregion

    #region reactions
    public async Task<List<Reaction>> GetReactions(RatingType type, string id, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(reactionsCollection)
            .WhereEqualTo("entityType", type)
            .WhereEqualTo("entityId", id)
            .GetSnapshotAsync(cancellationToken);
        var reactions = new List<Reaction>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<ReactionDto>();
            var reaction = RatingMapper.MapDto(dto);
            reactions.Add(reaction);
        }
        return reactions;
    }

    public async Task<Dictionary<string, List<Reaction>>> GetReactions(RatingType type, List<string> ids, CancellationToken cancellationToken)
    {
        if (ids == null || ids.Count == 0)
        {
            return [];
        }
        var snapshot = await db.Collection(reactionsCollection)
            .WhereEqualTo("entityType", type)
            .WhereIn("entityId", ids)
            .GetSnapshotAsync(cancellationToken);
        var reactions = new List<Reaction>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<ReactionDto>();
            var reaction = RatingMapper.MapDto(dto);
            reactions.Add(reaction);
        }
        var dict = reactions.GroupBy(x => x.EntityId).ToDictionary(x => x.Key, x => x.ToList());
        return dict;
    }

    public async Task<Reaction?> GetReaction(ReactionType reactionType, RatingType type, string id, string userId, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(reactionsCollection)
            .WhereEqualTo("reactionType", reactionType)
            .WhereEqualTo("entityType", type)
            .WhereEqualTo("entityId", id)
            .WhereEqualTo("userId", userId)
            .GetSnapshotAsync(cancellationToken);
        var doc = snapshot.FirstOrDefault();
        if (doc == null) return null;
        var dto = doc.ConvertTo<ReactionDto>();
        var reaction = RatingMapper.MapDto(dto);
        return reaction;
    }

    public async Task<Reaction?> GetReaction(string id, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(reactionsCollection).Document(id).GetSnapshotAsync(cancellationToken);
        if (!snapshot.Exists) return null;
        var dto = snapshot.ConvertTo<ReactionDto>();
        var reaction = RatingMapper.MapDto(dto);
        return reaction;
    }

    public async Task SaveReaction(Reaction reaction, CancellationToken cancellationToken)
    {
        var dto = RatingMapper.Map(reaction);
        await db.Collection(reactionsCollection).Document(dto.Id).SetAsync(dto, null, cancellationToken);
    }

    public async Task DeleteReaction(Reaction reaction, CancellationToken cancellationToken)
    {
        try
        {
            await db.Collection(reactionsCollection).Document(reaction.Id).DeleteAsync(null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to delete reaction with id {id}", reaction.Id);
            throw;
        }
    }
    #endregion


    #region ratings

    internal async Task<List<Rating>> GetAllRatings(CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(ratingCollection).GetSnapshotAsync(cancellationToken);
        var ratings = new List<Rating>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<RatingDto>();
            var rating = RatingMapper.MapDto(dto);
            ratings.Add(rating);
        }
        return ratings;
    }

    public async Task<List<Rating>> GetRatings(RatingType type, string id, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(ratingCollection).WhereEqualTo("entityType", type).WhereEqualTo("entityId", id).GetSnapshotAsync(cancellationToken);
        var ratings = new List<Rating>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<RatingDto>();
            var rating = RatingMapper.MapDto(dto);
            ratings.Add(rating);
        }
        return ratings;
    }

    public async Task<Dictionary<string, List<Rating>>> GetRatings(RatingType type, List<string> ids, CancellationToken cancellationToken)
    {
        if (ids == null || ids.Count == 0)
        {
            return new();
        }
        var snapshot = await db.Collection(ratingCollection).WhereEqualTo("entityType", type).WhereIn("entityId", ids).GetSnapshotAsync(cancellationToken);
        var ratings = new List<Rating>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<RatingDto>();
            var rating = RatingMapper.MapDto(dto);
            ratings.Add(rating);
        }
        var dict = ratings.GroupBy(x => x.EntityId).ToDictionary(x => x.Key, x => x.ToList());
        return dict;
    }

    public async Task<Rating?> GetRating(RatingType type, string id, string userId, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(ratingCollection).WhereEqualTo("entityType", type).WhereEqualTo("entityId", id).WhereEqualTo("userId", userId).GetSnapshotAsync(cancellationToken);
        var doc = snapshot.FirstOrDefault();
        if (doc == null) return null;
        var dto = doc.ConvertTo<RatingDto>();
        var rating = RatingMapper.MapDto(dto);
        return rating;
    }

    public async Task<Rating?> GetRating(string id, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(ratingCollection).Document(id).GetSnapshotAsync(cancellationToken);
        if (!snapshot.Exists) return null;
        var dto = snapshot.ConvertTo<RatingDto>();
        var rating = RatingMapper.MapDto(dto);
        return rating;
    }

    public async Task SaveRating(Rating rating, CancellationToken cancellationToken)
    {
        var dto = RatingMapper.Map(rating);
        await db.Collection(ratingCollection).Document(dto.Id).SetAsync(dto, null, cancellationToken);
    }

    public async Task DeleteRating(Rating rating, CancellationToken cancellationToken)
    {
        try
        {
            await db.Collection(ratingCollection).Document(rating.Id).DeleteAsync(null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete rating with id {id}", rating.Id);
            throw;
        }
    }


    #endregion

}

[FirestoreData]
public class RatingDto
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "";

    [FirestoreProperty("userId")]
    public string UserId { get; set; } = "";

    [FirestoreProperty("entityType")]
    public RatingTypeDto EntityType { get; set; }
    [FirestoreProperty("entityId")]
    public string EntityId { get; set; } = "";
    [FirestoreProperty("score")]
    public int Score { get; set; }
    [FirestoreProperty("comment")]
    public string? Comment { get; set; }
    [FirestoreProperty("approved")]
    public bool? Approved { get; set; } = true;


    [FirestoreDocumentCreateTimestamp]
    public DateTimeOffset CreatedAt { get; set; }

    [FirestoreDocumentUpdateTimestamp]
    public DateTimeOffset? UpdatedAt { get; set; }
}

public enum RatingTypeDto
{
    Recipe = 0,
}

public enum ReactionTypeDto
{
    Favorite = 0,
}

[FirestoreData]
public class ReactionDto
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "";

    [FirestoreProperty("userId")]
    public string UserId { get; set; } = "";

    [FirestoreProperty("entityType")]
    public RatingTypeDto EntityType { get; set; }

    [FirestoreProperty("entityId")]
    public string EntityId { get; set; } = "";

    [FirestoreProperty("reactionType")]
    public ReactionTypeDto ReactionType { get; set; }

    [FirestoreDocumentCreateTimestamp]
    public DateTimeOffset CreatedAt { get; set; }
}

[FirestoreData]
public class CommentDto
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "";

    [FirestoreProperty("userId")]
    public string UserId { get; set; } = "";

    [FirestoreProperty("entityType")]
    public RatingType EntityType { get; set; }

    [FirestoreProperty("entityId")]
    public string EntityId { get; set; } = "";

    [FirestoreProperty("message")]
    public string Message { get; set; } = "";

    [FirestoreProperty("editedAt")]
    public DateTimeOffset? EditedAt { get; set; }

    [FirestoreDocumentCreateTimestamp]
    public DateTimeOffset CreatedAt { get; set; }

    [FirestoreDocumentUpdateTimestamp]
    public DateTimeOffset? UpdatedAt { get; set; }

    [FirestoreProperty("deleted")]
    public DateTimeOffset? DeletedAt { get; set; }

    [FirestoreProperty("hidden")]
    public bool Hidden { get; set; }

    [FirestoreProperty("parentCommentId")]
    public string? ParentCommentId { get; set; }
}
