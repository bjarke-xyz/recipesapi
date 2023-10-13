using System.Collections;
using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace RecipesAPI.API.Infrastructure;

public class SqliteDataContext
{
    private readonly IConfiguration configuration;

    public SqliteDataContext(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        return new SqliteConnection(configuration.GetConnectionString("LocalData"));
    }

    public async Task Init()
    {
        using var connection = CreateConnection();
        await _initProductFeed();

        async Task _initProductFeed()
        {
            var sql = """
                CREATE TABLE IF NOT EXISTS
                AdtractionProductFeed (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    ProgramId INTEGER NOT NULL,
                    FeedId INTEGER NOT NULL,
                    FeedUrl TEXT NOT NULL,
                    LastUpdated TEXT NOT NULL,

                    UNIQUE(ProgramId, FeedId)
                );
                CREATE INDEX IF NOT EXISTS adtraction_product_feed_program_feed ON AdtractionProductFeed(ProgramId,FeedId);

                CREATE TABLE IF NOT EXISTS 
                AdtractionProductFeedItems (
                    AdtractionProductFeedId INTEGER,
                    Sku TEXT,
                    Name TEXT,
                    Description TEXT,
                    Category TEXT,
                    Price REAL,
                    Shipping TEXT,
                    Currency TEXT,
                    InStock BOOLEAN,
                    ProductUrl TEXT,
                    ImageUrl TEXT,
                    TrackingUrl TEXT,
                    Brand TEXT,
                    OriginalPrice TEXT,
                    Ean TEXT,
                    ManufacturerArticleNumber TEXT,
                    ExtrasJson TEXT
                );
                CREATE INDEX IF NOT EXISTS adtraction_product_feed_items_product_feed_id ON AdtractionProductFeedItems(AdtractionProductFeedId);
            """;
            await connection.ExecuteAsync(sql);
        }

    }
}