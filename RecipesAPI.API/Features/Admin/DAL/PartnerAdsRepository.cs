using System.ComponentModel;
using System.Data;
using System.Text;
using AutoMapper.Internal.Mappers;
using Dapper;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Admin.DAL;

public class PartnerAdsRepository(ILogger<PartnerAdsRepository> logger, SqliteDataContext context)
{
    public async Task<List<(string programId, string categoryName)>> GetCategories()
    {
        using var conn = context.CreateConnection();
        var sql =
        """
        select distinct programid, categoryname from PartnerAdsProductFeedItems fi
        join PartnerAdsProductFeed f on fi.PartnerAdsProductFeedId = f.id
        """;
        return (await conn.QueryAsync<(string programId, string categoryName)>(sql)).ToList();
    }

    public async Task<PartnerAdsProductFeedDto?> GetProductFeedInternal(IDbConnection conn, string programId, string feedLink)
    {
        var sql = """
        SELECT * FROM PartnerAdsProductFeed WHERE ProgramId = @programId AND FeedLink = @feedLink
        """;
        return await conn.QuerySingleOrDefaultAsync<PartnerAdsProductFeedDto>(sql, new { programId, feedLink });
    }

    public async Task<List<PartnerAdsFeedProduct>> GetFeedProducts(List<(string programId, string productId)> itemIdentifiers)
    {
        using var conn = context.CreateConnection();
        conn.Open();
        var dtos = new List<PartnerAdsProductFeedItemDto>();
        foreach (var (programId, productId) in itemIdentifiers)
        {
            var sql =
            """
        SELECT item.* FROM PartnerAdsProductFeedItems item
        JOIN PartnerAdsProductFeed feed ON item.PartnerAdsProductFeedId = feed.Id
        WHERE feed.ProgramId = @programId AND item.ProductId = @productId
        LIMIT 1
        """;
            var dto = await conn.QuerySingleOrDefaultAsync<PartnerAdsProductFeedItemDto>(sql, new { programId, productId });
            if (dto != null)
            {
                dtos.Add(dto);
            }
        }
        return dtos.Select(x => x.ToFeedProduct()).ToList();
    }

    public async Task<PartnerAdsFeedProduct?> GetFeedProduct(string programId, string productId)
    {
        using var conn = context.CreateConnection();
        var sql =
        """
        SELECT item.* FROM PartnerAdsProductFeedItems item
        JOIN PartnerAdsProductFeed feed ON item.PartnerAdsProductFeedId = feed.Id
        WHERE feed.ProgramId = @programId AND item.ProductId = @productId
        LIMIT 1
        """;
        var dto = await conn.QuerySingleOrDefaultAsync<PartnerAdsProductFeedItemDto>(sql, new { programId, productId });
        if (dto == null) return null;
        return dto.ToFeedProduct();
    }


    public async Task<List<PartnerAdsFeedProduct>> SearchFeedProducts(string? searchQuery, string? programId, int skip, int limit)
    {
        using var conn = context.CreateConnection();
        var sqlSb = new StringBuilder(
            """
            SELECT item.*, feed.ProgramId FROM PartnerAdsProductFeedItems item
            JOIN PartnerAdsProductFeed feed ON item.PartnerAdsProductFeedId = feed.Id
            """
        );
        var whereAdded = false;
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            sqlSb.Append(" WHERE ((ProductName LIKE ('%' || @query || '%')) OR (CategoryName LIKE ('%' || @query || '%')))");
            whereAdded = true;
        }

        if (!string.IsNullOrWhiteSpace(programId))
        {
            if (!whereAdded) sqlSb.Append(" WHERE ");
            if (whereAdded) sqlSb.Append(" AND ");
            sqlSb.Append(" feed.ProgramId = @programId");
        }
        sqlSb.AppendLine(" LIMIT @limit OFFSET @offset");
        var dtos = await conn.QueryAsync<PartnerAdsProductFeedItemDto>(sqlSb.ToString(), new { query = searchQuery, limit, offset = skip, programId });
        return dtos.Select(x => x.ToFeedProduct()).ToList();
    }

    public async Task<List<PartnerAdsFeedProduct>> GetFeedProducts(PartnerAdsProductFeedDto feedDto, int? skip, int? limit, string? searchQuery, int? afterId)
    {
        using var conn = context.CreateConnection();
        var sqlSb = new StringBuilder(
            """
            SELECT *, @programId as ProgramId FROM PartnerAdsProductFeedItems WHERE PartnerAdsProductFeedId = @id
            """
        );
        if (afterId.HasValue)
        {
            sqlSb.Append(" AND ItemId > @afterId");
        }
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            sqlSb.Append(" AND ProductName LIKE ('%' || @query || '%')");
        }
        sqlSb.Append(" ORDER BY ItemId ");
        if (limit.HasValue)
        {
            sqlSb.Append(" LIMIT @limit");
        }
        if (skip.HasValue)
        {
            sqlSb.Append(" OFFSET @offset");
        }
        var feedItemDtos = await conn.QueryAsync<PartnerAdsProductFeedItemDto>(
            sqlSb.ToString(), new { id = feedDto.Id, query = searchQuery, limit, offset = skip, programId = feedDto.ProgramId, afterId }
        );
        var feedProducts = feedItemDtos.Select(dto => dto.ToFeedProduct()).ToList();
        return feedProducts;
    }

    public async Task SaveProductFeed(string programId, string feedLink, DateTime feedUpdated, IAsyncEnumerable<PartnerAdsFeedProduct> feedProducts)
    {
        ArgumentNullException.ThrowIfNull(feedLink);
        using var conn = context.CreateConnection();
        try
        {
            var existingProductFeed = await GetProductFeedInternal(conn, programId, feedLink);
            if (existingProductFeed != null && feedUpdated <= existingProductFeed.FeedUpdated)
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
                    await conn.ExecuteAsync("DELETE FROM PartnerAdsProductFeed WHERE Id = @id", new { id = existingProductFeed.Id });
                    await conn.ExecuteAsync("DELETE FROM PartnerAdsProductFeedItems WHERE PartneradsProductFeedId = @id", new { id = existingProductFeed.Id });
                }

                var createdId = await conn.QuerySingleAsync<int>(
                    """
                INSERT INTO PartnerAdsProductFeed (ProgramId, FeedLink, FeedUpdated)
                VALUES (@ProgramId, @FeedLink, @FeedUpdated)
                RETURNING Id
                """, new PartnerAdsProductFeedDto(programId, feedLink, feedUpdated));

                await foreach (var product in feedProducts)
                {
                    var item = new PartnerAdsProductFeedItemDto(createdId, product);
                    await conn.ExecuteAsync("""
                INSERT INTO PartnerAdsProductFeedItems
                (PartnerAdsProductFeedId, Retailer, CategoryName, Brand, ProductName, ProductId, Description, NewPrice, OldPrice, DeliveryCost, StockQuantity, DeliveryTime, Size, ImageUrl, ProductUrl)
                VALUES
                (@PartnerAdsProductFeedId, @Retailer, @CategoryName, @Brand, @ProductName, @ProductId, @Description, @NewPrice, @OldPrice, @DeliveryCost, @StockQuantity, @DeliveryTime, @Size, @ImageUrl, @ProductUrl)
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

    public async Task<PartnerAdsProductFeedDto?> GetProductFeed(string programId, string feedLink)
    {
        using var conn = context.CreateConnection();
        return await GetProductFeedInternal(conn, programId, feedLink);
    }

}