using RecipesAPI.API.Utils;

namespace RecipesAPI.Tests;

public class SlugTests
{
    [TestCase("Pandekager", "pandekager")]
    [TestCase("Tiramisu", "tiramisu")]
    [TestCase("Italiensk vaniljeis", "italiensk-vaniljeis")]
    [TestCase("Tangzhong brød", "tangzhong-brod")]
    [TestCase("Test3", "test3")]
    public void UrlFriendly_ShouldGenerateSlug(string recipeTitle, string expectedSlug)
    {
        var slug = StringUtils.UrlFriendly(recipeTitle);
        Assert.That(expectedSlug, Is.EqualTo(slug));
    }
}