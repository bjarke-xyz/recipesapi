namespace RecipesAPI.API.Exceptions;

public class UserNotFoundException : GraphQLErrorException
{
    public UserNotFoundException(string? id = null, Exception? innerException = null) : base(id != null ? $"User with id {id} not found" : "User not found", innerException)
    {
    }
}