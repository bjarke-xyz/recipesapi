using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using RecipesAPI.API.Infrastructure;
using RecipesAPI.Tests.IntegrationTests;

namespace RecipesAPI.Tests;

public static class TestHelper
{
    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        var config = new TestConfig().GetConfiguration();
        var env = new TestWebHostEnvironment();
        services
            .AddDependencies(config, env)
            .AddLogging()
            .Replace(ServiceDescriptor.Singleton(sp => FirebaseTestHelper.GetDb()))
            .Replace(ServiceDescriptor.Singleton(sp => FirebaseTestHelper.GetAuth()))
            .Replace(ServiceDescriptor.Singleton<IStorageClient>(sp => new FileStorageClient("./data", "./metadata")));
        return services.BuildServiceProvider();
    }

    public static T GetRequiredService<T>() where T : notnull
    {
        var provider = Provider();
        return provider.GetRequiredService<T>();
    }
}

public class TestConfig
{
    private readonly Dictionary<string, string?> testConfig = new()
    {
        { "FirebaseRestApiBaseUrl", $"http://{FirebaseTestHelper.AuthHost}/identitytoolkit.googleapis.com" },
        { "FirebaseWebApiKey", "key" }
    };

    public IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder().AddInMemoryCollection(testConfig).Build();
    }
}

public class TestWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public IFileProvider WebRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string ContentRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string EnvironmentName { get => "TestEnvironment"; set => throw new NotImplementedException(); }
}