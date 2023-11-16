namespace RecipesAPI.API.Features.Ratings.Common;

public class Rating
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public RatingType EntityType { get; set; }
    public string EntityId { get; set; } = "";
    public int Score { get; set; }
    public string? Comment { get; set; }
    public bool? Approved { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public enum RatingType
{
    Recipe = 0
}

public class RatingHelper
{
    public const int MaxRating = 5;
    public const int MinRating = 1;
}

public enum ReactionType
{
    Favorite = 0,
}

public class Reaction
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public RatingType EntityType { get; set; }
    public string EntityId { get; set; } = "";
    public ReactionType ReactionType { get; set; }
}

public class Comment
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public RatingType EntityType { get; set; }
    public string EntityId { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTimeOffset? EditedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool Hidden { get; set; }
    public string? ParentCommentId { get; set; }

    // Populated in memory
    public List<Comment> Children { get; set; } = [];
}