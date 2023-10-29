using StackExchange.Redis;

namespace RecipesAPI.API.Infrastructure;

public static class RedisConnectionHelper
{
    private static IConnectionMultiplexer? connectionMultiplexer;

    public static IConnectionMultiplexer GetConnection(IConfiguration config)
    {
        connectionMultiplexer ??= ConnectionMultiplexer.Connect(GetConfigurationOptions(config));
        return connectionMultiplexer;
    }

    private static ConfigurationOptions GetConfigurationOptions(IConfiguration config)
    {
        var configuration = new StackExchange.Redis.ConfigurationOptions
        {
            User = config["REDIS_USER"],
            Password = config["REDIS_PASSWORD"],
            ClientName = "RecipesApi",
        };
        var redisHost = config["REDIS_HOST"];
        if (string.IsNullOrEmpty(redisHost))
        {
            throw new ArgumentNullException("REDIS_HOST must not be null");
        }
        configuration.EndPoints.Add(redisHost);
        return configuration;
    }

}