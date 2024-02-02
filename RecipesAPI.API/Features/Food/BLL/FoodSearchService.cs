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

namespace RecipesAPI.API.Features.Food.BLL;

public class FoodSearchService(ILogger<FoodSearchService> logger, string indexBasePath)
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
        writer.DeleteAll();
        logger.LogInformation("indexing food data, {count} items", foodItems.Count);
        var docs = foodItems.Select(source => new Document()
        {
            new Int32Field("id", source.FoodId, Field.Store.YES),
            new TextField("foodNameDa", TransformFoodName(source.FoodName.Da), Field.Store.YES),
            new TextField("foodNameEn", source.FoodName.En, Field.Store.YES)
        }).ToList();

        writer.AddDocuments(docs);
        writer.Flush(triggerMerge: false, applyAllDeletes: false);
        writer.Commit();
        logger.LogInformation("food data indexed, indexed {count} documents", writer.NumDocs);
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
        query.Boost = 10;
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

public record FoodItemSearchDoc(int FoodId, string FoodNameDa, string FoodNameEn, float Score);