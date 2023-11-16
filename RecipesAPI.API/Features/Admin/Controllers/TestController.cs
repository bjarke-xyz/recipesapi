using Microsoft.AspNetCore.Mvc;
using RecipesAPI.API.Features.Ratings.BLL;
using RecipesAPI.API.Features.Ratings.Common;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Admin.Controllers;

public class TestController(ILogger<TestController> logger, RatingsService ratingsService) : BaseController
{
    private readonly ILogger<TestController> logger = logger;
    private readonly RatingsService ratingsService = ratingsService;

    [HttpGet]
    public async Task<ActionResult> Test()
    {
        try
        {
            // await MigrateRatingsToReactions(HttpContext.RequestAborted);
            // await MigrateRatingsToComments(HttpContext.RequestAborted);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error");
            return StatusCode(500, ex.Message);
        }
    }

    private async Task MigrateRatingsToComments(CancellationToken cancellationToken)
    {
        var ratings = await ratingsService.GetAllRatings(cancellationToken);
        var ratingsWithComments = ratings.Where(x => !string.IsNullOrEmpty(x.Comment));

        var comments = ratingsWithComments.Select(rating => new Comment
        {
            UserId = rating.UserId,
            EntityType = rating.EntityType,
            EntityId = rating.EntityId,
            Message = rating.Comment ?? "",
            CreatedAt = rating.CreatedAt,
            UpdatedAt = rating.UpdatedAt,
            Hidden = rating.Approved == false,
        }).ToList();

        foreach (var comment in comments)
        {
            await ratingsService.SaveComment(comment, cancellationToken);
        }

    }
}