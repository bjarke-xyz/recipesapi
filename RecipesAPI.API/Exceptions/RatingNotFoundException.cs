namespace RecipesAPI.API.Exceptions;

public class RatingNotFoundException : GraphQLErrorException
{
    public RatingNotFoundException(string? id = null, Exception? innerException = null) : base(id != null ? $"Rating with id {id} not found" : "Rating not found", innerException)
    {
    }
}