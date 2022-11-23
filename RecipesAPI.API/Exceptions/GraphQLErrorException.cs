namespace RecipesAPI.API.Exceptions;

public class GraphQLErrorException : Exception
{
    public GraphQLErrorException(string message, Exception? innerException = null) : base(message ?? "Unknown error", innerException)
    {

    }
}