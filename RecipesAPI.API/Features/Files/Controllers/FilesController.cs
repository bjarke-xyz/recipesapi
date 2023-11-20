using Microsoft.AspNetCore.Mvc;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Files.Controllers;

public class FilesController(IStorageClient storageClient) : BaseController
{
    private readonly IStorageClient storageClient = storageClient;

    [HttpGet("file/file.{ext}")]
    public async Task<ActionResult> GetFile([FromRoute] string ext, [FromQuery] string bucket, [FromQuery] string key, CancellationToken cancellationToken)
    {
        var (stream, contentType) = await storageClient.GetStream(bucket, key, cancellationToken);
        if (stream == null || contentType == null)
        {
            return NotFound("file not found");
        }
        stream.Position = 0;
        Response.Headers.CacheControl = new Microsoft.Extensions.Primitives.StringValues("public, max-age=1800");
        return File(stream, contentType);
    }
}