using FirebaseAdmin.Auth;
using RecipesAPI.API.Features.Users.DAL;

namespace RecipesAPI.Tests.IntegrationTests;

public class UserTests
{
    [SetUp]
    public async Task SetUp()
    {
        await FirebaseTestHelper.ClearFirestore();
    }

    [Test]
    public async Task TestUserSync()
    {
        var auth = TestHelper.GetRequiredService<FirebaseAuth>();
        var userRecord = await auth.CreateUserAsync(new UserRecordArgs
        {
            DisplayName = "test1",
            Email = "test@example.org",
        });
        var userRepository = TestHelper.GetRequiredService<UserRepository>();

        var userInfosBeforeSync = await userRepository.GetUserInfos(CancellationToken.None);
        await userRepository.SyncUsers(null, CancellationToken.None);
        var userInfosAfterSync = await userRepository.GetUserInfos(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(userInfosBeforeSync, Has.Count.EqualTo(0));
            Assert.That(userInfosAfterSync, Has.Count.EqualTo(1));
        });

    }
}