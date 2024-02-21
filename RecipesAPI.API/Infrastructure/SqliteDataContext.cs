using System.Collections;
using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;

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

    public IDbConnection CreateCacheConnection()
    {
        return new SqliteConnection(configuration.GetConnectionString("SqliteCache"));
    }

    private void DeleteOldDb()
    {
        try
        {
            var oldDataSource = configuration.GetConnectionString("LocalDataOld");
            var parts = oldDataSource?.Split("=");
            if (parts?.Length >= 2)
            {
                var path = parts[1];
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "error deleting old db");
        }

    }

    public async Task Init()
    {
        DeleteOldDb();
        using var connection = CreateConnection();
        await _initProductFeed(connection);

        using var cacheConnection = CreateCacheConnection();
        await _initCache(cacheConnection);

        async static Task _initProductFeed(IDbConnection conn)
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
                    ItemId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
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
                CREATE INDEX IF NOT EXISTS adtraction_product_feed_items_sku ON AdtractionProductFeedItems(Sku);
                CREATE INDEX IF NOT EXISTS adtraction_product_feed_items_feed_id_sku ON AdtractionProductFeedItems(AdtractionProductFeedId, Sku);


                CREATE TABLE IF NOT EXISTS 
                PartnerAdsProductFeed (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    ProgramId TEXT NOT NULL,
                    FeedLink TEXT NOT NULL,
                    FeedUpdated TEXT NOT NULL,
                    UNIQUE(ProgramId, FeedLink)
                );
                CREATE INDEX IF NOT EXISTS partnerads_product_feed_program_feed ON PartnerAdsProductFeed(ProgramId,FeedLink);

                CREATE TABLE IF NOT EXISTS
                PartnerAdsProductFeedItems (
                    ItemId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    PartnerAdsProductFeedId INTEGER,
                    Retailer TEXT,
                    CategoryName TEXT,
                    Brand TEXT,
                    ProductName TEXT,
                    ProductId TEXT,
                    Description TEXT,
                    NewPrice REAL,
                    OldPrice REAL,
                    DeliveryCost TEXT,
                    StockQuantity TEXT,
                    DeliveryTime TEXT,
                    Size TEXT,
                    ImageUrl TEXT,
                    ProductUrl TEXT
                );
                CREATE INDEX IF NOT EXISTS partnerads_product_feed_items_product_feed_id ON PartnerAdsProductFeedItems(PartnerAdsProductFeedId);
                CREATE INDEX IF NOT EXISTS partnerads_product_feed_items_product_id ON PartnerAdsProductFeedItems(ProductId);
                CREATE INDEX IF NOT EXISTS partnerads_product_feed_items_product_feed_id_product_id ON PartnerAdsProductFeedItems(PartnerAdsProductFeedId, ProductId);
            """;
            await conn.ExecuteAsync(sql);
        }

        async static Task _initCache(IDbConnection conn)
        {
            var sql = """
                CREATE TABLE IF NOT EXISTS kv (
                    Key TEXT PRIMARY KEY,
                    Val BLOB,
                    CreatedAt INTEGER,
                    ExpireAt INTEGER
                );
                CREATE INDEX IF NOT EXISTS index_expire_kv ON kv(ExpireAt);
            """;
            await conn.ExecuteAsync(sql);
        }

    }
}