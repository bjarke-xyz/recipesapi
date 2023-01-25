namespace RecipesAPI.API.Exceptions;

public class RecipeNotFoundException : GraphQLErrorException
{
    public RecipeNotFoundException(string? id = null, Exception? innerException = null) : base(id != null ? $"Recipe with id {id} not found" : "Recipe not found", innerException)
    {
    }
}