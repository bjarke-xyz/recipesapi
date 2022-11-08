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

    public async Task<SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24>> LoadImage(Stream fileContent, CancellationToken cancellationToken)
    {
        var image = await SixLabors.ImageSharp.Image.LoadAsync<SixLabors.ImageSharp.PixelFormats.Rgb24>(fileContent, cancellationToken);
        return image;
    }

    public (string blurHash, int width, int height) GetBlurHash(SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24> image)
    {
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
        return (blurHash, width, height);
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
        int blurHashWidth = -1;
        int blurHashHeight = -1;
        int originalWidth = -1;
        int originalHeight = -1;
        try
        {
            using var image = await LoadImage(fileContent, cancellationToken);
            originalWidth = image.Width;
            originalHeight = image.Height;
            (blurHash, blurHashWidth, blurHashHeight) = GetBlurHash(image);
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
        if (blurHashWidth == -1 || blurHashHeight == -1 || originalWidth == -1 || originalHeight == -1)
        {
            logger.LogError("failed to get width/height values");
            throw new Exception("failed to get width/height values");
        }

        file.BlurHash = blurHash;
        file.Dimensions = new Files.DAL.ImageDimensionsDto
        {
            BlurHash = new Files.DAL.ImageDimensionDto
            {
                Width = blurHashWidth,
                Height = blurHashHeight,
            },
            Original = new Files.DAL.ImageDimensionDto
            {
                Width = originalWidth,
                Height = originalHeight
            }
        };
        await fileService.SaveFile(file, cancellationToken);
    }

}