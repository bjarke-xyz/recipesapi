using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Newtonsoft.Json;
using RecipesAPI.Exceptions;
using RecipesAPI.Infrastructure;

namespace RecipesAPI.Food.DAL;

public class FoodRepository
{
    private readonly S3StorageClient storageClient;

    public FoodRepository(S3StorageClient storageClient)
    {
        this.storageClient = storageClient;
    }

    public async Task<List<FoodDto>> GetFoodData(CancellationToken cancellationToken)
    {
        var stream = await this.storageClient.GetStream("frida", "data.csv", cancellationToken);
        if (stream == null)
        {
            throw new GraphQLErrorException("No frida data found");
        }
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<FoodDto>();
        return records.ToList();
    }
}

public class FoodDto
{
    [Name("FoodID")]
    public int FoodId { get; set; }

    [Name("FødevareNavn")]
    public string FoodNameDanish { get; set; } = default!;

    [Name("FoodName")]
    public string FoodName { get; set; } = default!;

    [Name("ParameterID")]
    public int ParameterId { get; set; }

    [Name("ParameterNavn")]
    public string ParameterNameDanish { get; set; } = default!;

    [Name("ParameterName")]
    public string ParameterName { get; set; } = default!;

    [Name("SortKey")]
    public int SortKey { get; set; }

    [Name("ResVal")]
    public double? ResVal { get; set; }

    [Name("Source")]
    public string? Source { get; set; } = default!;

    [Name("SourceFood")]
    public int? SourceFood { get; set; }
}