using System.Data;
using System.Text;
using Dapper;
using RecipesAPI.API.Features.Admin.Common.Adtraction;
using RecipesAPI.API.Infrastructure;
using StackExchange.Redis;

namespace RecipesAPI.API.Features.Admin.DAL;

public class AdtractionRepository(SqliteDataContext context, ILogger<AdtractionRepository> logger)
{
    private readonly ILogger<AdtractionRepository> logger = logger;

    private readonly SqliteDataContext context = context;

    private static async Task<AdtractionProductFeedDto?> GetProductFeedInternal(IDbConnection conn, int programId, int feedId)
    {
        var sql = """
        SELECT * FROM AdtractionProductFeed WHERE ProgramId = @programId AND FeedId = @feedId
        """;
        return await conn.QuerySingleOrDefaultAsync<AdtractionProductFeedDto>(sql, new { programId, feedId });
    }

    public async Task<AdtractionFeedProduct?> GetFeedProduct(int programId, int feedId, string sku)
    {
        using var conn = context.CreateConnection();
        var sql =
         """
         SELECT item.* FROM AdtractionProductFeedItems item 
         JOIN AdtractionProductFeed feed ON item.AdtractionProductFeedId = feed.Id
         WHERE item.Sku = @sku AND feed.ProgramId = @programId AND feed.FeedId = @feedId
         LIMIT 1
         """;
        var dto = await conn.QueryFirstOrDefaultAsync<AdtractionProductFeedItemDto>(sql, new { programId, feedId, sku });
        if (dto == null) return null;
        dto.ProgramId = programId;
        dto.FeedId = feedId;
        return dto.ToFeedProduct();
    }

    public async Task<List<AdtractionFeedProduct>> SearchFeedProducts(string? searchQuery, int? programId, int skip, int limit)
    {
        using var conn = context.CreateConnection();
        var sqlSb = new StringBuilder(
            """
            SELECT item.*, feed.ProgramId, feed.FeedId FROM AdtractionProductFeedItems item
            JOIN AdtractionProductFeed feed ON item.AdtractionProductFeedId = feed.Id
            """
        );
        var whereAdded = false;
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            whereAdded = true;
            sqlSb.Append(" WHERE ((Name LIKE ('%' || @query || '%')) OR (Category LIKE ('%' || @query || '%')))");
        }

        if (programId.HasValue)
        {
            if (!whereAdded) sqlSb.Append(" WHERE ");
            if (whereAdded) sqlSb.Append(" AND ");
            sqlSb.Append(" feed.ProgramId = @programId");
        }
        sqlSb.AppendLine(" LIMIT @limit OFFSET @offset");
        var dtos = await conn.QueryAsync<AdtractionProductFeedItemDto>(sqlSb.ToString(), new { query = searchQuery, limit, offset = skip, programId });
        return dtos.Select(x => x.ToFeedProduct()).ToList();
    }

    public async Task<List<AdtractionFeedProduct>> GetFeedProducts(AdtractionProductFeedDto feedDto, int? skip, int? limit, string? searchQuery)
    {
        using var conn = context.CreateConnection();
        var sqlSb = new StringBuilder(
            """
            SELECT * FROM AdtractionProductFeedItems WHERE AdtractionProductFeedId = @id
            """
        );
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            sqlSb.Append(" AND Name LIKE ('%' || @query || '%')");
        }
        if (limit.HasValue)
        {
            sqlSb.Append(" LIMIT @limit");
        }
        if (skip.HasValue)
        {
            sqlSb.Append(" OFFSET @offset");
        }
        var feedItemDtos = await conn.QueryAsync<AdtractionProductFeedItemDto>(
            sqlSb.ToString(), new { id = feedDto.Id, query = searchQuery, limit, offset = skip }
        );
        foreach (var dto in feedItemDtos)
        {
            dto.ProgramId = feedDto.ProgramId;
            dto.FeedId = feedDto.FeedId;
        }
        var feedProducts = feedItemDtos.Select(dto => dto.ToFeedProduct()).ToList();
        return feedProducts;
    }

    public async Task SaveProductFeed(int programId, Feed feed, List<AdtractionFeedProduct> feedProducts)
    {
        ArgumentNullException.ThrowIfNull(feed.FeedId);
        ArgumentNullException.ThrowIfNull(feed.LastUpdated);
        using var conn = context.CreateConnection();
        try
        {
            var existingProductFeed = await GetProductFeedInternal(conn, programId, feed.FeedId.Value);
            if (existingProductFeed != null && feed.LastUpdated.Value <= existingProductFeed.LastUpdated)
            {
                logger.LogInformation("Not updating product feed, no changes");
                return;
            }

            conn.Open();
            var tx = conn.BeginTransaction();
            try
            {
                if (existingProductFeed != null)
                {
                    await conn.ExecuteAsync("DELETE FROM AdtractionProductFeed WHERE Id = @id", new { id = existingProductFeed.Id });
                    await conn.ExecuteAsync("DELETE FROM AdtractionProductFeedItems WHERE AdtractionProductFeedId = @id", new { id = existingProductFeed.Id });
                }

                var createdId = await conn.QuerySingleAsync<int>(
                    """
                INSERT INTO AdtractionProductFeed (ProgramId, FeedId, FeedUrl, LastUpdated)
                VALUES (@ProgramId, @FeedId, @FeedUrl, @LastUpdated)
                RETURNING Id
                """, new AdtractionProductFeedDto(programId, feed));

                var feedItems = feedProducts
                    .Select(p => new AdtractionProductFeedItemDto(createdId, p))
                    .ToList();

                foreach (var item in feedItems)
                {
                    await conn.ExecuteAsync("""
                INSERT INTO AdtractionProductFeedItems
                (AdtractionProductFeedId, Sku, Name, Description, Category, Price, Shipping, Currency, InStock, ProductUrl, ImageUrl, TrackingUrl, Brand, OriginalPrice, Ean, ManufacturerArticleNumber, ExtrasJson)
                VALUES
                (@AdtractionProductFeedId, @Sku, @Name, @Description, @Category, @Price, @Shipping, @Currency, @InStock, @ProductUrl, @ImageUrl, @TrackingUrl, @Brand, @OriginalPrice, @Ean, @ManufacturerArticleNumber, @ExtrasJson)
                """, item);
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SaveProductFeed tx failed");
                tx.Rollback();
            }

        }
        finally
        {
            conn.Close();
        }
    }

    public async Task<AdtractionProductFeedDto?> GetProductFeed(int programId, int feedId)
    {
        using var conn = context.CreateConnection();
        return await GetProductFeedInternal(conn, programId, feedId);
    }
}