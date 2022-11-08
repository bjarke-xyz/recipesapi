using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RecipesAPI.Files.BLL;
using RecipesAPI.Recipes.BLL;

namespace RecipesAPI.Tests;

public class ImageProcessingTests
{
    private Mock<IFileService>? mockFileService;
    private IServiceProvider? serviceProvider;

    [SetUp]
    public void Setup()
    {
        mockFileService = new Mock<IFileService>();
        serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
    }

    [TestCase("cat.jpg")]
    [TestCase("bike.jpg")]
    public async Task BlurHash_ShouldReturnNonEmptyString(string file)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(mockFileService);
        // Arrange
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ImageProcessingService>();
        var imageProcessingService = new ImageProcessingService(mockFileService.Object, logger);

        Stream fileContent = new FileStream($"../../../test-data/{file}", FileMode.Open);

        // Act
        var blurHash = await imageProcessingService.GetBlurHash(fileContent, CancellationToken.None);

        // Assert
        Assert.IsNotEmpty(blurHash);
    }
}