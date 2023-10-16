using System.Data;
using System.Text;
using AutoMapper.Internal.Mappers;
using Dapper;
using RecipesAPI.API.Features.Admin.Common;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Admin.DAL;

public class PartnerAdsRepository
{
    private readonly ILogger<PartnerAdsRepository> logger;
    private readonly SqliteDataContext context;

    public PartnerAdsRepository(ILogger<PartnerAdsRepository> logger, SqliteDataContext context)
    {
        this.logger = logger;
        this.context = context;
    }

    public async Task<PartnerAdsProductFeedDto?> GetProductFeedInternal(IDbConnection conn, string programId, string feedLink)
    {
        var sql = """
        SELECT * FROM PartnerAdsProductFeed WHERE ProgramId = @programId AND FeedLink = @feedLink
        """;
        return await conn.QuerySingleOrDefaultAsync<PartnerAdsProductFeedDto>(sql, new { programId, feedLink });
    }

    public async Task<List<PartnerAdsFeedProduct>> GetFeedProducts(PartnerAdsProductFeedDto feedDto, int? skip, int? limit, string? searchQuery)
    {
        using var conn = context.CreateConnection();
        var sqlSb = new StringBuilder(
            """
            SELECT * FROM PartnerAdsProductFeedItems WHERE PartnerAdsProductFeedId = @id
            """
        );
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            sqlSb.Append(" AND ProductName LIKE ('%' || @query || '%')");
        }
        if (limit.HasValue)
        {
            sqlSb.Append(" LIMIT @limit");
        }
        if (skip.HasValue)
        {
            sqlSb.Append(" OFFSET @offset");
        }
        var feedItemDtos = await conn.QueryAsync<PartnerAdsProductFeedItemDto>(
            sqlSb.ToString(), new { id = feedDto.Id, query = searchQuery, limit, offset = skip }
        );
        var feedProducts = feedItemDtos.Select(dto => dto.ToFeedProduct()).ToList();
        return feedProducts;
    }

    public async Task SaveProductFeed(string programId, string feedLink, DateTime feedUpdated, List<PartnerAdsFeedProduct> feedProducts)
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

                var feedItems = feedProducts
                    .Select(p => new PartnerAdsProductFeedItemDto(createdId, p))
                    .ToList();

                foreach (var item in feedItems)
                {
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