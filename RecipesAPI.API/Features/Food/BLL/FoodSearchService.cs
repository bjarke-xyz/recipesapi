using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using RecipesAPI.API.Features.Food.Common;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Da;
using Hangfire.Server;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace RecipesAPI.API.Features.Food.BLL;

public class FoodSearchServiceV2(ILogger<FoodSearchServiceV2> logger, string indexBasePath)
{
    const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
    private static readonly StandardAnalyzer standardAnalyzer = new(AppLuceneVersion);
    private static readonly Dictionary<string, Lucene.Net.Analysis.Analyzer> fieldAnalyzers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["foodNameDa"] = new DanishAnalyzer(AppLuceneVersion),
        ["foodNameEn"] = standardAnalyzer,
    };
    private static readonly PerFieldAnalyzerWrapper perFieldAnalyzerWrapper = new(standardAnalyzer, fieldAnalyzers);
    private readonly string indexBasePath = indexBasePath;
    private IndexWriter? writer = null;

    private IndexWriter GetWriter()
    {
        if (writer != null)
        {
            return writer;
        }
        var indexPath = System.IO.Path.Combine(indexBasePath, "food");
        var dir = FSDirectory.Open(indexPath);

        // Create an index writer
        var indexConfig = new IndexWriterConfig(AppLuceneVersion, perFieldAnalyzerWrapper);
        writer = new IndexWriter(dir, indexConfig);
        return writer;
    }

    public void IndexData(IReadOnlyList<FoodItem> foodItems)
    {
        var writer = GetWriter();
        logger.LogInformation("indexing food data, {count} items", foodItems.Count);
        var sw = Stopwatch.StartNew();
        writer.DeleteAll();
        var docs = foodItems.Select(source => new Document()
        {
            new Int32Field("id", source.FoodId, Field.Store.YES),
            new TextField("foodNameDa", TransformFoodName(source.FoodName.Da), Field.Store.YES),
            new TextField("foodNameEn", source.FoodName.En, Field.Store.YES)
        }).ToList();

        writer.AddDocuments(docs);
        writer.Flush(triggerMerge: false, applyAllDeletes: false);
        writer.Commit();
        sw.Stop();
        logger.LogInformation("food data indexed, indexed {count} documents in {ms}ms", writer.NumDocs, sw.ElapsedMilliseconds);
    }

    private static Regex parensRegex = new(@"\(.*\)");
    private static string TransformFoodName(string foodName)
    {
        var match = parensRegex.Match(foodName);
        if (match.Success)
        {
            foodName = foodName.Replace(match.Value, "");
        }
        return foodName;
    }

    public List<FoodItemSearchDoc> Search(string queryString)
    {
        var writer = GetWriter();
        using var reader = writer.GetReader(applyAllDeletes: true);
        var searcher = new IndexSearcher(reader);
        var queryParser = new QueryParser(AppLuceneVersion, "foodNameDa", perFieldAnalyzerWrapper);
        var query = queryParser.Parse(queryString);
        var hits = searcher.Search(query, 10);
        // if (hits.TotalHits == 0)
        // {
        //     queryParser = new QueryParser(AppLuceneVersion, "foodNameEn", perFieldAnalyzerWrapper);
        //     query = queryParser.Parse(queryString);
        //     hits = searcher.Search(query, 10);
        // }
        var result = new List<FoodItemSearchDoc>();
        foreach (var scoreDoc in hits.ScoreDocs)
        {
            var doc = searcher.Doc(scoreDoc.Doc);
            var foodItemSearchDoc = new FoodItemSearchDoc(int.Parse(doc.Get("id")), doc.Get("foodNameDa"), doc.Get("foodNameEn"), scoreDoc.Score);
            result.Add(foodItemSearchDoc);
        }
        return result;
    }
}

public class FoodSearchServiceV1
{
    public List<FoodItemSearchDoc> Search(IReadOnlyList<FoodItem> foodData, string queryString)
    {
        var queue = new PriorityQueue<FoodItem, int>();
        foreach (var item in foodData)
        {
            var (matched, rank) = HasMatch(item.FoodName.Da, queryString);
            if (matched)
            {
                queue.Enqueue(item, rank);
            }
            else
            {
                (matched, rank) = HasMatch(item.FoodName.En, queryString);
                if (matched)
                {
                    queue.Enqueue(item, rank);
                }
            }
        }
        var result = new List<FoodItemSearchDoc>();
        while (queue.Count > 0)
        {
            var potentialFood = queue.Dequeue();
            var foodItemSearchDoc = new FoodItemSearchDoc(potentialFood, queue.Count);
            result.Add(foodItemSearchDoc);
        }
        return result;
    }

    private readonly Regex percentageRegex = new Regex(@"\w+(\d+\s*%)$");

    private (bool match, int rank) HasMatch(string foodName, string query)
    {
        if (query.Contains("fløde", StringComparison.OrdinalIgnoreCase) && foodName.Contains("fløde", StringComparison.OrdinalIgnoreCase))
        {

        }
        var foodNameParts = foodName.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        for (var i = 0; i < foodNameParts.Count; i++)
        {
            var foodNamePart = foodNameParts[i];
            var percentageMatch = percentageRegex.Match(foodNamePart);
            if (percentageMatch != null && percentageMatch.Success)
            {
                if (!query.Contains("%"))
                {
                    foodNamePart = foodNamePart.Replace(percentageMatch.Groups[0].Value, "").Trim();
                }
                else
                {
                    foodNamePart = foodNamePart.Replace(percentageMatch.Groups[0].Value, percentageMatch.Groups[0].Value.Replace(" ", "")).Trim();
                }
            }
            var extraScore = i * 10;
            if (string.Equals(foodNamePart, query))
            {
                return (true, 0 + extraScore);
            }
            else if (string.Equals(foodNamePart, query, StringComparison.OrdinalIgnoreCase))
            {
                return (true, 10 + extraScore);
            }
            else if (foodNamePart.Contains(query))
            {
                return (true, 20 + extraScore);
            }
            else if (foodNamePart.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return (true, 30 + extraScore);
            }
        }

        if (string.Equals(foodName, query))
        {
            return (true, 1000);
        }
        else if (string.Equals(foodName, query, StringComparison.OrdinalIgnoreCase))
        {
            return (true, 2000);
        }
        else if (foodName.Contains(query))
        {
            return (true, 3000);
        }
        else if (foodName.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return (true, 4000);
        }

        if (query.Contains(" "))
        {
            var queryParts = query.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Reverse().ToList();
            var spaceRank = 1;
            foreach (var queryPart in queryParts)
            {
                var (matched, rank) = HasMatch(foodName, queryPart);
                if (matched)
                {
                    return (true, rank + spaceRank);
                }
                spaceRank++;
            }
        }

        return (false, int.MaxValue);
    }
}


public record FoodItemSearchDoc(int FoodId, string FoodNameDa, string FoodNameEn, float Score)
{
    public FoodItemSearchDoc(FoodItem foodItem, float score) : this(foodItem.FoodId, foodItem.FoodName.Da, foodItem.FoodName.En, score) { }
}