namespace RecipesAPI.API.Exceptions;

public class EquipmentNotFoundException : GraphQLErrorException
{
    public EquipmentNotFoundException(string? id = null, Exception? innerException = null) : base(id != null ? $"Equipment with id {id} not found" : "Equipment not found", innerException)
    {
    }
}