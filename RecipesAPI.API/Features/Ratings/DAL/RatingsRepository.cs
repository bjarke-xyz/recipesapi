using Google.Cloud.Firestore;
using RecipesAPI.API.Features.Ratings.Common;

namespace RecipesAPI.API.Features.Ratings.DAL;

public class RatingsRepository
{
    private readonly ILogger<RatingsRepository> logger;
    private readonly FirestoreDb db;
    private const string ratingCollection = "ratings";

    public RatingsRepository(ILogger<RatingsRepository> logger, FirestoreDb db)
    {
        this.logger = logger;
        this.db = db;
    }

    public async Task<List<Rating>> GetRatings(RatingType type, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(ratingCollection).WhereEqualTo("entityType", type).GetSnapshotAsync(cancellationToken);
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

    [FirestoreDocumentCreateTimestamp]
    public DateTimeOffset CreatedAt { get; set; }

    [FirestoreDocumentUpdateTimestamp]
    public DateTimeOffset? UpdatedAt { get; set; }
}

public enum RatingTypeDto
{
    Recipe = 0,
}