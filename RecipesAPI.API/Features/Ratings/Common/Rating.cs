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