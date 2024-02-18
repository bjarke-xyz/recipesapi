using RecipesAPI.API.Features.Files.BLL;
using RecipesAPI.API.Features.Files.DAL;
using RecipesAPI.API.Features.Recipes.Common;

namespace RecipesAPI.API.Features.Recipes.BLL;

public class ImageService(IFileService fileService, ImageProcessingService imageProcessingService, IConfiguration config)
{
    public Image? GetImage(Recipe recipe, FileDto file)
    {
        var originalDimensions = file.Dimensions?.Original;
        var thumbnails = new ImageThumbnails();
        var sizes = new List<ThumbnailSize> { ThumbnailSize.Small, ThumbnailSize.Medium, ThumbnailSize.Large };
        foreach (var thumbnailSize in sizes)
        {
            var thumbnailDto = file.GetImageThumbnail(thumbnailSize);
            if (thumbnailDto == null && originalDimensions != null)
            {
                var baseUrl = config["ApiUrl"];
                var generateThumbnailApiUrl = $"{baseUrl}/api/recipes/thumbnail/{recipe.Id}?thumbnailSize={thumbnailSize}";
                var (width, height, _) = imageProcessingService.GetImageThumbnailDimensions(originalDimensions.Width, originalDimensions.Height, thumbnailSize);
                var thumbnail = new ImageThumbnail
                {
                    Size = 0,
                    Src = generateThumbnailApiUrl,
                    Type = file.ContentType,
                    ThumbnailSize = thumbnailSize,
                    Dimensions = new ImageDimension
                    {
                        Width = width,
                        Height = height,
                    },
                };
                thumbnails.SetThumbnail(thumbnailSize, thumbnail);
            }
            else if (thumbnailDto != null)
            {
                var thumbnailSrc = fileService.GetPublicUrl(file.Bucket, thumbnailDto.Key, thumbnailDto.ContentType);
                var thumbnail = new ImageThumbnail
                {
                    Size = thumbnailDto.Size,
                    Type = thumbnailDto.ContentType,
                    Src = thumbnailSrc,
                    ThumbnailSize = thumbnailSize,
                    Dimensions = RecipeMapper.MapDto(thumbnailDto.Dimensions),
                };
                thumbnails.SetThumbnail(thumbnailSize, thumbnail);
            }
        }
        var imageSrc = fileService.GetPublicUrl(file);
        var image = new Image
        {
            ImageId = recipe.ImageId!,
            Name = file.FileName,
            Size = file.Size,
            Type = file.ContentType,
            Src = imageSrc,
            Thumbnails = thumbnails,
            Dimensions = RecipeMapper.MapDto(file.Dimensions),
        };
        return image;
    }
}