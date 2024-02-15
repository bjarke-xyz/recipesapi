using System.Diagnostics;
using System.Text;
using Lucene.Net.Analysis.Da;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using RecipesAPI.API.Features.Recipes.Common;

namespace RecipesAPI.API.Features.Recipes.BLL;

public class RecipeSearchService(ILogger<RecipeSearchService> logger, string indexBasePath)
{
    const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
    private static readonly Lucene.Net.Analysis.Analyzer analyzer = new DanishAnalyzer(AppLuceneVersion);
    private IndexWriter? writer = null;

    private IndexWriter GetWriter()
    {
        if (writer != null)
        {
            return writer;
        }
        var indexPath = System.IO.Path.Combine(indexBasePath, "recipes");
        var dir = FSDirectory.Open(indexPath);

        // Create an index writer
        var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
        writer = new IndexWriter(dir, indexConfig);
        return writer;
    }

    private string FormatParts(List<RecipePart> parts)
    {
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            sb.AppendLine(part.Title);
            foreach (var step in part.Steps)
            {
                sb.AppendLine("\t" + step);
            }
            foreach (var ingredient in part.Ingredients)
            {
                sb.AppendLine("\t" + ingredient.Original);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public void IndexData(List<Recipe> recipes)
    {
        var writer = GetWriter();
        logger.LogInformation("indexing recipes, {count} items", recipes.Count);
        var sw = Stopwatch.StartNew();
        writer.DeleteAll();
        var docs = recipes.Select(source => new Document()
        {
            new StringField("id", source.Id, Field.Store.YES),
            new TextField("title", source.Title, Field.Store.YES),
            new TextField("description", source.Description ?? "", Field.Store.YES),
            new TextField("tips", string.Join("\n", source.Tips ?? []), Field.Store.YES),
            new TextField("parts", FormatParts(source.Parts), Field.Store.YES),
        }).ToList();

        writer.AddDocuments(docs);
        writer.Flush(triggerMerge: false, applyAllDeletes: false);
        writer.Commit();
        sw.Stop();
        logger.LogInformation("recipes indexed, indexed {count} docs in {ms}ms", writer.NumDocs, sw.ElapsedMilliseconds);
    }

    public List<RecipeSearchDoc> Search(string queryString, bool searchPartsAndTips, int count)
    {
        var writer = GetWriter();
        using var reader = writer.GetReader(applyAllDeletes: true);
        var searcher = new IndexSearcher(reader);
        var searchFields = new List<string> { "title", "description" };
        if (searchPartsAndTips)
        {
            searchFields.AddRange(["tips", "parts"]);
        }
        var queryParser = new MultiFieldQueryParser(AppLuceneVersion, [.. searchFields], analyzer);
        var query = queryParser.Parse(queryString);
        var hits = searcher.Search(query, count);
        var results = hits.ScoreDocs.Select(scoreDoc =>
        {
            var doc = searcher.Doc(scoreDoc.Doc);
            var recipeSearchDoc = new RecipeSearchDoc(doc.Get("id"), doc.Get("title"), scoreDoc.Score);
            return recipeSearchDoc;
        }).ToList();
        return results;
    }
}

public record RecipeSearchDoc(string Id, string Title, float Score);

