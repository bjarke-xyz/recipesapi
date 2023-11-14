using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Newtonsoft.Json;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Food.DAL;

public class FoodRepository
{
    private readonly string fridaCsvFilePath;

    public FoodRepository(string fridaCsvFilePath)
    {
        this.fridaCsvFilePath = fridaCsvFilePath;
    }

    public Task<List<FoodDto>> GetFoodData(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(fridaCsvFilePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<FoodDto>();
        return Task.FromResult(records.ToList());
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

    [Name("Min")]
    public string? Min { get; set; }

    [Name("Max")]
    public string? Max { get; set; }

    [Name("Median")]
    public string? Median { get; set; }

    [Name("NumberOfDeterminations")]
    public string? NumberOfDeterminations { get; set; }

    [Name("Sources")]
    public string? Source { get; set; }

    [Name("SourceFoodID")]
    public int? SourceFood { get; set; }

    [Name("o")]
    public string? O { get; set; }

    [Name("p")]
    public string? P { get; set; }
}