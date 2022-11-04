using RecipesAPI.Files.BLL;
using SixLabors.ImageSharp.Processing;

namespace RecipesAPI.Recipes.BLL;

public class ImageProcessingService
{
    private readonly IFileService fileService;
    private readonly ILogger<ImageProcessingService> logger;

    public ImageProcessingService(IFileService fileService, ILogger<ImageProcessingService> logger)
    {
        this.fileService = fileService;
        this.logger = logger;
    }
    public async Task<string> GetBlurHash(Stream fileContent, CancellationToken cancellationToken)
    {
        using var image = await SixLabors.ImageSharp.Image.LoadAsync<SixLabors.ImageSharp.PixelFormats.Rgb24>(fileContent, cancellationToken);
        var width = 300;
        var height = 300;
        var aspectRatio = (double)image.Width / (double)image.Height;
        var doResize = true;
        if (image.Width > width)
        {
            height = (int)(width / aspectRatio);
        }
        else if (image.Height > height)
        {
            width = (int)(height * aspectRatio);
        }
        else
        {
            doResize = false;
        }

        if (doResize)
        {
            image.Mutate(x => x.Resize(width, height));
        }
        var blurHash = Blurhash.ImageSharp.Blurhasher.Encode(image, 4, 3);
        return blurHash;
    }

    public async ValueTask ProcessRecipeImage(string imageId, CancellationToken cancellationToken)
    {
        var file = await fileService.GetFile(imageId, cancellationToken);
        if (file == null)
        {
            logger.LogWarning("File with id {id} was null", imageId);
            return;
        }
        var fileContent = await fileService.GetFileContent(imageId, cancellationToken);
        if (fileContent == null)
        {
            logger.LogWarning("File content with id {id} was null", imageId);
            return;
        }
        fileContent.Position = 0;

        string blurHash = "";
        try
        {
            blurHash = await GetBlurHash(fileContent, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "blur hash failed");
            throw;
        }
        if (string.IsNullOrEmpty(blurHash))
        {
            logger.LogError("blur hash was empty");
            throw new Exception("blur hash was empty");
        }

        file.BlurHash = blurHash;
        await fileService.SaveFile(file, cancellationToken);
    }

}