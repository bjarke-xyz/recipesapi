using System.Diagnostics;

namespace RecipesAPI.API.Infrastructure;
public static class Telemetry
{
    public const string ServiceName = "RecipesAPI.API";
    public static ActivitySource ActivitySource { get; } = new(ServiceName);
}