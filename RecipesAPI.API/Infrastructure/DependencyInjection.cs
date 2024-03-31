using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using RecipesAPI.API.Auth;
using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Admin.DAL;
using RecipesAPI.API.Features.Equipment.BLL;
using RecipesAPI.API.Features.Equipment.DAL;
using RecipesAPI.API.Features.Files.BLL;
using RecipesAPI.API.Features.Files.DAL;
using RecipesAPI.API.Features.Food.BLL;
using RecipesAPI.API.Features.Food.DAL;
using RecipesAPI.API.Features.Healthcheck.BLL;
using RecipesAPI.API.Features.Ratings.BLL;
using RecipesAPI.API.Features.Ratings.DAL;
using RecipesAPI.API.Features.Recipes.BLL;
using RecipesAPI.API.Features.Recipes.DAL;
using RecipesAPI.API.Features.Users.BLL;
using RecipesAPI.API.Features.Users.DAL;
using Serilog;

namespace RecipesAPI.API.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        return services
            .AddSingleton<JwtUtil>(sp => new JwtUtil(config["FirebaseAppId"]!, sp.GetRequiredService<ICacheProvider>()))
            .AddSingleton<RecipesAPI.API.Infrastructure.Serializers.ISerializer>(sp =>
            {
                var serializerTag = config["SQLITE_CACHE_SERIALIZER"];
                Log.Information("serializer: {serializer}", serializerTag);
                return serializerTag switch
                {
                    Serializers.MyJsonSerializer.Tag => new RecipesAPI.API.Infrastructure.Serializers.MyJsonSerializer(),
                    _ or Serializers.MyMessagePackSerializer.Tag => new RecipesAPI.API.Infrastructure.Serializers.MyMessagePackSerializer(),
                };
            })
            .AddSingleton<ICacheProvider, SqliteCacheProvider>()
            .AddSingleton<SqliteCacheProvider>()
            .AddSingleton<IStorageClient, GoogleStorageClient>()
            .AddSingleton<IEmailService, EmailService>(sp =>
            {
                var apiUrl = config["EMAILSERVICE_URL"]!;
                var apiUser = config["EMAILSERVICE_USER"]!;
                var apiPassword = config["EMAILSERVICE_PASSWORD"]!;
                var logger = sp.GetRequiredService<ILogger<EmailService>>();
                return new EmailService(apiUrl, apiUser, apiPassword, logger);
            })
            .AddSingleton(sp =>
            {
                return FirestoreDb.Create(config["FirebaseAppId"]);
            })
            .AddSingleton(sp =>
            {
                return FirebaseAuth.DefaultInstance;
            })
            .AddSingleton(sp =>
            {
                return Google.Cloud.Storage.V1.StorageClient.Create();
            })
            .AddSingleton(sp =>
            {
                var db = sp.GetRequiredService<FirestoreDb>();
                var auth = sp.GetRequiredService<FirebaseAuth>();
                return new UserRepository(config["FirebaseRestApiBaseUrl"]!, config["FirebaseWebApiKey"]!, db, auth, sp.GetRequiredService<ILogger<UserRepository>>());
            })
            .AddSingleton<FoodRepository>(sp =>
            {
                var csvPath = "../scripts/frida/output/final/frida.csv";
                if (env.IsProduction())
                {
                    csvPath = config["FridaCsvPath"];
                    if (string.IsNullOrEmpty(csvPath))
                    {
                        csvPath = "/data/frida.csv";
                    }
                }
                return new FoodRepository(csvPath);
            })
            .AddSingleton<FoodService>()
            .AddSingleton(sp =>
            {
                return new FoodSearchServiceV2(sp.GetRequiredService<ILogger<FoodSearchServiceV2>>(), config["SearchIndexPath"]!);
            })
            .AddSingleton<FoodSearchServiceV1>()
            .AddSingleton(sp =>
            {
                return new AffiliateSearchServiceV2(sp.GetRequiredService<ILogger<AffiliateSearchServiceV2>>(), config["SearchIndexPath"]!);
            })
            .AddSingleton(sp =>
            {
                return new RecipeSearchService(sp.GetRequiredService<ILogger<RecipeSearchService>>(), config["SearchIndexPath"]!);
            })
            .AddSingleton<AffiliateSearchServiceV1>()
            .AddSingleton<UserService>()
            .AddSingleton<ICacheKeyGetter>(sp => sp.GetRequiredService<UserService>())
            .AddSingleton<RecipeRepository>()
            .AddSingleton<RecipeService>()
            .AddSingleton<ICacheKeyGetter>(sp => sp.GetRequiredService<RecipeService>())
            .AddSingleton<ParserService>()
            .AddSingleton<FileRepository>()
            .AddSingleton<SettingsService>()
            .AddSingleton<IFileService, FileService>(sp =>
            {
                var fileRepository = sp.GetRequiredService<FileRepository>();
                var cacheProvider = sp.GetRequiredService<ICacheProvider>();
                var storageClient = sp.GetRequiredService<IStorageClient>();
                var logger = sp.GetRequiredService<ILogger<FileService>>();
                return new FileService(fileRepository, cacheProvider, storageClient, logger, GoogleStorageClient.StorageBucket, config["ApiUrl"]!);
            })
            .AddSingleton<AdminService>()
            .AddSingleton<ImageProcessingService>(sp =>
            {
                var fileService = sp.GetRequiredService<IFileService>();
                var logger = sp.GetRequiredService<ILogger<ImageProcessingService>>();
                var storageClient = sp.GetRequiredService<IStorageClient>();
                return new ImageProcessingService(fileService, logger, storageClient, GoogleStorageClient.StorageBucket);
            })
            .AddSingleton<HealthcheckService>()
            .AddSingleton<EquipmentRepository>()
            .AddSingleton<EquipmentService>()
            .AddSingleton<RatingsRepository>()
            .AddSingleton<RatingsService>()
            .AddSingleton<PartnerAdsRepository>()
            .AddSingleton<PartnerAdsService>(sp =>
            {
                var url = config["PartnerAdsUrl"] ?? "";
                var key = config["PartnerAdsKey"] ?? "";
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(PartnerAdsService));
                var logger = sp.GetRequiredService<ILogger<PartnerAdsService>>();
                var partnerAdsRepository = sp.GetRequiredService<PartnerAdsRepository>();
                return new PartnerAdsService(url, key, httpClient, logger, partnerAdsRepository, sp.GetRequiredService<ICacheProvider>());
            })
            .AddSingleton<AdtractionRepository>()
            .AddSingleton<AdtractionService>(sp =>
            {
                var url = config["AdtractionApiUrl"] ?? "";
                var key = config["AdtractionApiKey"] ?? "";
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(AdtractionService));
                var logger = sp.GetRequiredService<ILogger<AdtractionService>>();
                var adtractionRepository = sp.GetRequiredService<AdtractionRepository>();
                var defaultMarket = "DK";
                var defaultChannelId = config.GetValue<int>("AdtractionChannelId");
                var cache = sp.GetRequiredService<ICacheProvider>();
                return new AdtractionService(logger, url, key, httpClient, adtractionRepository, defaultMarket, defaultChannelId, cache);
            })
            .AddSingleton<ImageService>()
            .AddSingleton<CacheJobService>()
            .AddSingleton<AffiliateService>()
            .AddSingleton<SqliteDataContext>()
            .AddSingleton<SettingsRepository>()
            .AddSingleton<SettingsService>();
    }
}