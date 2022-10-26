using Microsoft.AspNetCore.Mvc;
using RecipesAPI.Files.BLL;
using Serilog;

namespace RecipesAPI.Files.Http;

public static class ImageHandler
{
    public static async Task<IResult> GetImage(string id, [FromServices] FileService fileService, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var file = await fileService.GetFile(id, cancellationToken);
            if (file == null) return Results.NotFound();
            var content = await fileService.GetFileContent(file.Id, cancellationToken);
            if (content == null) return Results.NotFound();
            httpContext.Response.Headers.CacheControl = "public, max-age=86400";
            return Results.Stream(content, file.ContentType, null, file.CreatedAt);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get image");
            return Results.StatusCode(500);
        }
    }
}