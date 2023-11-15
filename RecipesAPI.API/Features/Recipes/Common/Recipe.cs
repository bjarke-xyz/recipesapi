
using Microsoft.Extensions.ObjectPool;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Features.Files.DAL;
using RecipesAPI.API.Features.Ratings.Common;
using RecipesAPI.API.Features.Recipes.BLL;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Features.Recipes.Common;

public class Recipe
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageId { get; set; }
    public string UserId { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public bool Published { get; set; }
    public List<string> Tips { get; set; } = default!;
    public string? Yield { get; set; }
    public List<RecipePart> Parts { get; set; } = default!;
    public int? Difficulty { get; set; }
    public List<string> Slugs { get; set; } = default!;
    public string? Slug => (Slugs ?? new List<string>()).LastOrDefault();

    public Recipe? Draft { get; set; }

    public List<string> EquipmentIds { get; set; } = new List<string>();

    public RecipeRating? Rating { get; set; } = null;
}

public class RecipeAuthor
{
    public string UserId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Name => DisplayName;
}

public class RecipePart
{
    public string Title { get; set; } = default!;
    public List<RecipeIngredient> Ingredients { get; set; } = default!;
    public List<string> Steps { get; set; } = default!;
    public double? TimeEstimate { get; set; }
}

public class RecipeIngredient
{
    public string Original { get; set; } = default!;
    public string? Title { get; set; }
    public double? Volume { get; set; }
    public string? Unit { get; set; }
    public List<string> Meta { get; set; } = default!;
    public bool ManuallyEntered { get; set; }
    public bool Optional { get; set; }
    public List<AffiliateItemReference> AffiliateItemReferences { get; set; } = [];
}

public class Image
{
    public string ImageId { get; set; } = default!;
    public string Src { get; set; } = default!;
    public string Type { get; set; } = default!;
    public long Size { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ImageDimensions? Dimensions { get; set; }
    public ImageThumbnails? Thumbnails { get; set; }
}

public class ImageDimensions
{
    public ImageDimension? Original { get; set; }
}

public class ImageDimension
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public class ImageThumbnail
{
    public ThumbnailSize ThumbnailSize { get; set; }
    public ImageDimension? Dimensions { get; set; } = new();
    public string Src { get; set; } = default!;
    public string Type { get; set; } = default!;
    public long Size { get; set; } = default!;
}

public class ImageThumbnails
{
    public ImageThumbnail? Small { get; set; }
    public ImageThumbnail? Medium { get; set; }
    public ImageThumbnail? Large { get; set; }

    public void SetThumbnail(ThumbnailSize thumbnailSize, ImageThumbnail thumbnail)
    {
        switch (thumbnailSize)
        {
            case ThumbnailSize.Small:
                this.Small = thumbnail;
                break;
            case ThumbnailSize.Large:
                this.Large = thumbnail;
                break;
            default:
                this.Medium = thumbnail;
                break;
        }
    }
}

public class RecipeStats
{
    public int RecipeCount { get; set; }
    public int ChefCount { get; set; }
}

public class RecipeRating
{
    public double Score { get; set; }
    public int Raters { get; set; }
}

public class RecipeReactions
{
    public RecipeReactions()
    {
    }

    public RecipeReactions(List<Reaction> reactions, User? loggedInUser)
    {
        var favoritesReactions = reactions.Where(x => x.ReactionType == ReactionType.Favorite).ToList();
        FavoritesCount = favoritesReactions.Count;
        UserHasFavorited = favoritesReactions.Any(x => x.UserId == loggedInUser?.Id);
    }

    public int FavoritesCount { get; set; }
    public bool? UserHasFavorited { get; set; }
}

public class ExtendedRecipeRating
{

    public ExtendedRecipeRating() { }
    public ExtendedRecipeRating(List<Rating> ratings, User? loggedInUser)
    {
        Ratings = ratings;

        for (var i = RatingHelper.MinRating; i <= RatingHelper.MaxRating; i++)
        {
            RatingGroups.Add(new RecipeRating { Score = i, Raters = 0 });
        }
        RatingGroups.Reverse();
        foreach (var rating in ratings)
        {
            var rg = RatingGroups.FirstOrDefault(x => (int)x.Score == rating.Score);
            if (rg != null)
            {
                rg.Raters++;
            }
        }

        if (loggedInUser != null)
        {
            UserRating = Ratings.FirstOrDefault(x => x.UserId == loggedInUser.Id);
        }

        if (loggedInUser == null || !loggedInUser.HasRole(Role.MODERATOR))
        {
            foreach (var rating in Ratings)
            {
                if (loggedInUser != null && loggedInUser.Id == rating.UserId)
                {
                    continue;
                }
                if (rating.Approved == false)
                {
                    rating.Comment = null;
                }
            }
        }

    }
    public List<RecipeRating> RatingGroups { get; set; } = [];
    public List<Rating> Ratings { get; set; } = [];
    public Rating? UserRating { get; set; } = null;
}