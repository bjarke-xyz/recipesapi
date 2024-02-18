using RecipesAPI.API.Features.Files.BLL;
using RecipesAPI.API.Features.Files.DAL;
using RecipesAPI.API.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace RecipesAPI.API.Features.Recipes.BLL;

public class ImageProcessingService
{
    private readonly IFileService fileService;
    private readonly ILogger<ImageProcessingService> logger;
    private readonly IStorageClient storageClient;
    private readonly string storageBucket;

    public ImageProcessingService(IFileService fileService, ILogger<ImageProcessingService> logger, IStorageClient storageClient, string storageBucket)
    {
        this.fileService = fileService;
        this.logger = logger;
        this.storageClient = storageClient;
        this.storageBucket = storageBucket;
    }

    public async Task<(SixLabors.ImageSharp.Image, SixLabors.ImageSharp.Formats.IImageFormat?)> LoadImage(Stream fileContent, CancellationToken cancellationToken)
    {
        var image = await SixLabors.ImageSharp.Image.LoadAsync(fileContent, cancellationToken);
        return (image, image?.Metadata?.DecodedImageFormat);
    }

    public (int width, int height, bool requiresResize) GetImageThumbnailDimensions(int imageWidth, int imageHeight, ThumbnailSize thumbnailSize)
    {
        var (width, height) = thumbnailSize switch
        {
            ThumbnailSize.Small => (50, 50),
            ThumbnailSize.Large => (500, 500),
            _ => (300, 300),
        };
        var aspectRatio = (double)imageWidth / (double)imageHeight;
        var requiresResize = true;
        if (imageWidth > width)
        {
            height = (int)(width / aspectRatio);
        }
        else if (imageHeight > height)
        {
            width = (int)(height * aspectRatio);
        }
        else
        {
            width = imageWidth;
            height = imageHeight;
            requiresResize = false;
        }
        return (width, height, requiresResize);
    }

    private void ResizeImage(SixLabors.ImageSharp.Image image, ThumbnailSize thumbnailSize)
    {
        var (width, height, doResize) = GetImageThumbnailDimensions(image.Width, image.Height, thumbnailSize);
        if (doResize)
        {
            image.Mutate(x => x.Resize(width, height));
        }
    }

    private async Task<Stream?> ConvertToWebP(SixLabors.ImageSharp.Image image, SixLabors.ImageSharp.Formats.IImageFormat imageFormat, CancellationToken cancellationToken)
    {
        if (string.Equals(imageFormat.DefaultMimeType, "image/webp", StringComparison.OrdinalIgnoreCase)) return null;
        var ms = new MemoryStream();
        await image.SaveAsWebpAsync(ms, cancellationToken);
        ms.Position = 0;
        return ms;
    }

    private async Task<(SixLabors.ImageSharp.Image? image, SixLabors.ImageSharp.Formats.IImageFormat? format, Stream imageContent, bool needsReSaving)> LoadAndConvertImage(Stream originalFileContent, CancellationToken cancellationToken)
    {
        var (originalImage, originalFormat) = await LoadImage(originalFileContent, cancellationToken);
        if (originalImage == null || originalFormat == null) return (null, null, originalFileContent, false);
        var webpFileContent = await ConvertToWebP(originalImage, originalFormat, cancellationToken);
        if (webpFileContent == null)
        {
            return (originalImage, originalFormat, originalFileContent, false);
        }
        originalImage.Dispose();
        originalFileContent.Dispose();
        var (webpImage, webpFormat) = await LoadImage(webpFileContent, cancellationToken);
        return (webpImage, webpFormat, webpFileContent, true);
    }

    public async ValueTask ProcessRecipeImage(string imageId, string recipeId, ThumbnailSize thumbnailSize, CancellationToken cancellationToken)
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

        ImageThumbnailDto? thumbnailDto = null;
        int originalWidth = -1;
        int originalHeight = -1;
        SixLabors.ImageSharp.Image? image = null;
        SixLabors.ImageSharp.Formats.IImageFormat? format = null;

        // convert to webp if necessary
        try
        {
            Stream imageContent;
            bool needsReSaving;
            (image, format, imageContent, needsReSaving) = await LoadAndConvertImage(fileContent, cancellationToken);
            if (image == null)
            {
                throw new Exception("Failed to load image, image was null");
            }
            if (format == null)
            {
                throw new Exception("Unable to detect format of image");
            }
            if (needsReSaving)
            {
                file.Size = imageContent.Length;
                file.ContentType = format.DefaultMimeType;
                await fileService.SaveFile(file, imageContent, cancellationToken);
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "image conversion failed");
            throw;
        }

        try
        {
            // Store original image width and height before resizing to thumbnail size
            originalWidth = image.Width;
            originalHeight = image.Height;
            ResizeImage(image, thumbnailSize);
            var thumbnailKey = $"recipes/{recipeId}/thumbnails/{thumbnailSize}";
            var encoder = SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager.GetEncoder(format);
            using var imageMs = new MemoryStream();
            image.Save(imageMs, encoder);
            await storageClient.PutStream(storageBucket, thumbnailKey, imageMs, format.DefaultMimeType, cancellationToken);
            thumbnailDto = new ImageThumbnailDto
            {
                ThumbnailSize = thumbnailSize,
                ContentType = format.DefaultMimeType,
                Key = thumbnailKey,
                Size = imageMs.Length,
                Dimensions = new ImageDimensionDto
                {
                    Height = image.Height,
                    Width = image.Width,
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "thumbnail generation failed failed");
            throw;
        }
        finally
        {
            image?.Dispose();
        }
        if (thumbnailDto == null)
        {
            logger.LogError("thumbnailDto was null");
            throw new Exception("Failed to generate thumbnail");
        }

        if (file.Thumbnails == null) file.Thumbnails = new();
        switch (thumbnailSize)
        {
            case ThumbnailSize.Small:
                file.Thumbnails.Small = thumbnailDto;
                break;
            case ThumbnailSize.Large:
                file.Thumbnails.Large = thumbnailDto;
                break;
            default:
                file.Thumbnails.Medium = thumbnailDto;
                break;
        }
        file.Dimensions = new Files.DAL.ImageDimensionsDto
        {
            Original = new Files.DAL.ImageDimensionDto
            {
                Width = originalWidth,
                Height = originalHeight
            }
        };
        await fileService.SaveFile(file, cancellationToken);
    }


}