
namespace RecipesAPI.API.Features.Admin.Common.Adtraction;

public class AdtractionError
{
    public string? Message { get; set; }
    public int? Status { get; set; }
}

//"{\"message\":\"Invalid status. Expected value: 0 = live, 3 = closing\",\"status\":400}"