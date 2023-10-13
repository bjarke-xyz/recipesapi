using FirebaseAdmin;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using RecipesAPI.API;
using RecipesAPI.API.Auth;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Infrastructure;
using StackExchange.Redis;
using Serilog;
using Prometheus;
using Hangfire;
using Hangfire.Dashboard;
using System.Globalization;
using RecipesAPI.API.Features.Users.DAL;
using RecipesAPI.API.Features.Food.DAL;
using RecipesAPI.API.Features.Food.BLL;
using RecipesAPI.API.Features.Users.BLL;
using RecipesAPI.API.Features.Recipes.DAL;
using RecipesAPI.API.Features.Recipes.BLL;
using RecipesAPI.API.Features.Files.DAL;
using RecipesAPI.API.Features.Admin.BLL;
using RecipesAPI.API.Features.Files.BLL;
using RecipesAPI.API.Features.Food;
using RecipesAPI.API.Features.Users.Graph;
using RecipesAPI.API.Features.Recipes.Graph;
using RecipesAPI.API.Features.Food.Graph;
using RecipesAPI.API.Features.Admin.Graph;
using RecipesAPI.API.Features.Healthcheck.BLL;
using RecipesAPI.API.Features.Equipment.Graph;
using RecipesAPI.API.Features.Equipment.DAL;
using RecipesAPI.API.Features.Equipment.BLL;
using Serilog.Events;
using Serilog.Formatting.Compact;
using RecipesAPI.API.Features.Ratings.DAL;
using RecipesAPI.API.Features.Ratings.BLL;
using RecipesAPI.API.Features.Admin.DAL;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();



var googleAppCredContent = builder.Configuration["GOOGLE_APPLICATION_CREDENTIALS_CONTENT"];
if (!string.IsNullOrEmpty(googleAppCredContent))
{
    await File.WriteAllTextAsync("/tmp/serviceaccount.json", googleAppCredContent);
    System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "/tmp/serviceaccount.json");
}

var port = 5001;
if (int.TryParse(builder.Configuration["PORT"], out var _port)) port = _port;

builder.WebHost.UseKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(port);
});

builder.Host.UseSerilog(Log.Logger);

var storageBucket = "recipes-5000.appspot.com";

FirebaseApp.Create();

StackExchange.Redis.ConfigurationOptions GetRedisConfigurationOptions(WebApplicationBuilder b)
{
    var configuration = new StackExchange.Redis.ConfigurationOptions
    {
        User = b.Configuration["REDIS_USER"],
        Password = b.Configuration["REDIS_PASSWORD"],
        ClientName = "RecipesApi",
    };
    var redisHost = b.Configuration["REDIS_HOST"];
    if (string.IsNullOrEmpty(redisHost))
    {
        throw new ArgumentNullException("REDIS_HOST must not be null");
    }
    configuration.EndPoints.Add(redisHost);
    return configuration;
}

var jwtUtil = new JwtUtil(builder.Configuration["FirebaseAppId"]!, null);

builder.Services
    .AddHttpClient()
    .AddControllers().AddControllersAsServices().Services
    .AddRouting()
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = jwtUtil.GetAuthority();
            options.TokenValidationParameters = jwtUtil.GetTokenValidationParameters();
        }).Services
    .AddAuthorization()
    .AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseInMemoryStorage()
    )
    .AddHangfireServer()
    .AddHangfireAuthorizationMiddleware()
    .AddSingleton<JwtUtil>(sp => new JwtUtil(builder.Configuration["FirebaseAppId"]!, sp.GetRequiredService<ICacheProvider>()))
    .AddSingleton<ICacheProvider, CacheProvider>(sp =>
    {
        var distributedCache = sp.GetRequiredService<IDistributedCache>();
        var keyPrefix = builder.Configuration["REDIS_PREFIX"]!;
        var redis = sp.GetRequiredService<IConnectionMultiplexer>();
        return new CacheProvider(distributedCache, keyPrefix, redis);
    })
    .AddSingleton<S3StorageClient>(sp =>
    {
        var r2AccountId = builder.Configuration["R2_ACCOUNTID"]!;
        var r2AccessKeySecret = builder.Configuration["R2_ACCESSKEYSECRET"]!;
        var r2AccessKeyId = builder.Configuration["R2_ACCESSKEYID"]!;
        return new S3StorageClient(r2AccessKeyId, r2AccessKeySecret, r2AccountId);
    })
    .AddSingleton<IStorageClient, GoogleStorageClient>()
    .AddSingleton<IEmailService, EmailService>(sp =>
    {
        var apiUrl = builder.Configuration["EMAILSERVICE_URL"]!;
        var apiUser = builder.Configuration["EMAILSERVICE_USER"]!;
        var apiPassword = builder.Configuration["EMAILSERVICE_PASSWORD"]!;
        var logger = sp.GetRequiredService<ILogger<EmailService>>();
        return new EmailService(apiUrl, apiUser, apiPassword, logger);
    })
    .AddSingleton(sp =>
    {
        return FirestoreDb.Create(builder.Configuration["FirebaseAppId"]);
    })
    .AddSingleton(sp =>
    {
        return Google.Cloud.Storage.V1.StorageClient.Create();
    })
    .AddSingleton(sp =>
    {
        var db = sp.GetRequiredService<FirestoreDb>();
        return new UserRepository(builder.Configuration["FirebaseWebApiKey"]!, db, sp.GetRequiredService<ILogger<UserRepository>>());
    })
    .AddSingleton<FoodRepository>(sp =>
    {
        var csvPath = "../scripts/frida/output/final/frida.csv";
        if (builder.Environment.IsProduction())
        {
            csvPath = "/data/frida.csv";
        }
        return new FoodRepository(csvPath);
    })
    .AddSingleton<FoodService>()
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
        return new FileService(fileRepository, cacheProvider, storageClient, logger, storageBucket);
    })
    .AddSingleton<AdminService>()
    .AddSingleton<ImageProcessingService>(sp =>
    {
        var fileService = sp.GetRequiredService<IFileService>();
        var logger = sp.GetRequiredService<ILogger<ImageProcessingService>>();
        var storageClient = sp.GetRequiredService<IStorageClient>();
        return new ImageProcessingService(fileService, logger, storageClient, storageBucket);
    })
    .AddSingleton<HealthcheckService>()
    .AddSingleton<EquipmentRepository>()
    .AddSingleton<EquipmentService>()
    .AddSingleton<RatingsRepository>()
    .AddSingleton<RatingsService>()
    .AddSingleton<PartnerAdsService>(sp =>
    {
        var url = builder.Configuration["PartnerAdsUrl"] ?? "";
        var key = builder.Configuration["PartnerAdsKey"] ?? "";
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(PartnerAdsService));
        var logger = sp.GetRequiredService<ILogger<PartnerAdsService>>();
        return new PartnerAdsService(url, key, httpClient, logger);
    })
    .AddSingleton<AdtractionRepository>()
    .AddSingleton<AdtractionService>(sp =>
    {
        var url = builder.Configuration["AdtractionApiUrl"] ?? "";
        var key = builder.Configuration["AdtractionApiKey"] ?? "";
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(AdtractionService));
        var logger = sp.GetRequiredService<ILogger<AdtractionService>>();
        var adtractionRepository = sp.GetRequiredService<AdtractionRepository>();
        return new AdtractionService(logger, url, key, httpClient, adtractionRepository);
    })
    .AddSingleton<RequestInfoService>()
    .AddSingleton<SqliteDataContext>()
    .AddHostedService<CacheRefreshBackgroundService>()
    .AddHostedService<AutoStopperBackgroundService>()
    .AddHostedService<HangfireRecurringJobs>()
    .AddHttpContextAccessor()
    .AddSingleton<IConnectionMultiplexer>(sp =>
    {
        return ConnectionMultiplexer.Connect(GetRedisConfigurationOptions(builder));
    })
    .AddStackExchangeRedisCache(options =>
    {
        options.ConfigurationOptions = GetRedisConfigurationOptions(builder);
    })
    .AddDistributedMemoryCache()
    .AddCors()
    .AddGraphQLServer()
        .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment())
        .AddDiagnosticEventListener<ErrorLoggingDiagnosticsEventListener>()
        .AddErrorFilter(error =>
        {
            Log.Error(error.Exception, "Exception was thrown");
            if (error.Exception is GraphQLErrorException)
            {
                return error.WithMessage(error.Exception.Message).RemoveExtensions();
            }
            else
            {
                return error.WithMessage("An error has occurred");
            }
        })
        .AddAuthorization()
        .AddHttpRequestInterceptor<AuthInterceptor>()
        .AddQueryType()
        .AddMutationType()
            // Users
            .AddTypeExtension<UserQueries>()
            .AddTypeExtension<ExtendedUserQueries>()
            .AddTypeExtension<ExtendedSimpleUserQueries>()
            .AddTypeExtension<UserMutations>()
            // Recipes
            .AddTypeExtension<RecipeQueries>()
            .AddTypeExtension<RecipeMutations>()
            .AddTypeExtension<RecipeIngredientQueries>()
            .AddTypeExtension<ExtendedRecipeQueries>()
            // Food
            .AddTypeExtension<FoodQueries>()
            // Admin
            .AddTypeExtension<AdminQueries>()
            .AddTypeExtension<AdtractionFeedQueries>()
            .AddTypeExtension<AdminMutations>()
            // Equipment
            .AddTypeExtension<EquipmentQueries>()
            .AddTypeExtension<EquipmentMutations>()
        .AddType<UploadType>()
;

var app = builder.Build();

{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        await context.Init();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "failed to initialise sqlite");
    }
}

app.Use((ctx, next) =>
{
    HttpRequestRewindExtensions.EnableBuffering(ctx.Request);
    var requestInfoService = ctx.RequestServices.GetRequiredService<RequestInfoService>();
    requestInfoService.OnNewRequestReceived(ctx.Request);
    return next(ctx);
});

app.UseCors(o => o
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin());

app
    .UseWebSockets()
    .UseHttpMetrics(options =>
    {
        options.ReduceStatusCodeCardinality();
    })
    .UseSerilogRequestLogging()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseWhen((ctx => ctx.Request.Path.StartsWithSegments("/hangfire")), hangfireApp => hangfireApp.UseHangfireAuthorizationMiddleware())
    .UseEndpoints(endpoint =>
    {
        endpoint.MapGet("/", (ctx) =>
        {
            ctx.Response.Redirect("/graphql");
            return Task.CompletedTask;
        });
        endpoint.MapControllers();
        endpoint.MapGraphQL();
        endpoint.MapHangfireDashboard(new DashboardOptions { Authorization = new List<IDashboardAuthorizationFilter> { new HangfireDashboardAuthorizationFilter() } });
    });

try
{
    Log.Information("Starting API");
    var metricServer = new KestrelMetricServer(port: 1234);
    metricServer.Start();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shutdown complete");
    Log.CloseAndFlush();
}
