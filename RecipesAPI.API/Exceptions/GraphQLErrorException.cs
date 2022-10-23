namespace RecipesAPI.Exceptions;

public class GraphQLErrorException : Exception
{
    public GraphQLErrorException(string message) : base(message ?? "Unknown error")
    {

    }
}