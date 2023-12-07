using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using RecipesAPI.API;
using Snapshooter.NUnit;

namespace RecipesAPI.Tests;

public class GraphQLSchemaTests
{
    [Test]
    public async Task SchemaChangeTest()
    {
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddRecipesAPITypes()
            .BuildSchemaAsync();
        schema.ToString().MatchSnapshot();
    }

}