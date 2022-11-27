using Newtonsoft.Json;

namespace RecipesAPI.API.Utils;

public static class Extensions
{
    public static T? DeepClone<T>(this T? obj)
    {
        var jsonStr = JsonConvert.SerializeObject(obj);
        return JsonConvert.DeserializeObject<T>(jsonStr);
    }
}