using FirebaseAdmin;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using RecipesAPI.API;
using RecipesAPI.Auth;
using RecipesAPI.Exceptions;
using RecipesAPI.Food;
using RecipesAPI.Food.DAL;
using RecipesAPI.Food.Graph;
using RecipesAPI.Infrastructure;
using RecipesAPI.Recipes.BLL;
using RecipesAPI.Recipes.DAL;
using RecipesAPI.Recipes.Graph;
using RecipesAPI.Users;
using RecipesAPI.Users.BLL;
using RecipesAPI.Users.DAL;
using RecipesAPI.Users.Graph;
using StackExchange.Redis;
using Serilog;
using RecipesAPI.Files.DAL;
using RecipesAPI.Files.BLL;
using RecipesAPI.Admin.BLL;
using RecipesAPI.Admin.Graph;

DotNetEnv.Env.Load();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();


var builder = WebApplication.CreateBuilder(args);

var port = 5001;
if (int.TryParse(builder.Configuration["PORT"], out var _port)) port = _port;

builder.WebHost.UseKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(port);
});

builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console());

FirebaseApp.Create();

StackExchange.Redis.ConfigurationOptions GetRedisConfigurationOptions(WebApplicationBuilder b)
{
    var configuration = new StackExchange.Redis.ConfigurationOptions
    {
        User = b.Configuration["REDIS_USER"],
        Password = b.Configuration["REDIS_PASSWORD"],
        ClientName = "RecipesApi",
    };
    configuration.EndPoints.Add($"{b.Configuration["REDIS_HOST"]}:{b.Configuration["REDIS_PORT"]}");
    return configuration;
}

builder.Services
    .AddRouting()
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://securetoken.google.com/{builder.Configuration["FirebaseAppId"]}";
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://securetoken.google.com/{builder.Configuration["FirebaseAppId"]}",
                ValidateAudience = true,
                ValidAudience = builder.Configuration["FirebaseAppId"],
                ValidateLifetime = true,
            };
        }).Services
    .AddAuthorization()
    .AddSingleton<ICacheProvider, CacheProvider>(sp =>
    {
        var distributedCache = sp.GetRequiredService<IDistributedCache>();
        var keyPrefix = builder.Configuration["REDIS_PREFIX"];
        var redis = sp.GetRequiredService<IConnectionMultiplexer>();
        return new CacheProvider(distributedCache, keyPrefix, redis);
    })
    .AddSingleton<S3StorageClient>(sp =>
    {
        var r2AccountId = builder.Configuration["R2_ACCOUNTID"];
        var r2AccessKeySecret = builder.Configuration["R2_ACCESSKEYSECRET"];
        var r2AccessKeyId = builder.Configuration["R2_ACCESSKEYID"];
        return new S3StorageClient(r2AccessKeyId, r2AccessKeySecret, r2AccountId);
    })
    .AddSingleton<IStorageClient, GoogleStorageClient>()
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
        return new UserRepository(builder.Configuration["FirebaseWebApiKey"], db);
    })
    .AddSingleton<FoodRepository>()
    .AddSingleton<FoodService>()
    .AddSingleton<UserService>()
    .AddSingleton<ICacheKeyGetter>(sp => sp.GetRequiredService<UserService>())
    .AddSingleton<RecipeRepository>()
    .AddSingleton<RecipeService>()
    .AddSingleton<ICacheKeyGetter>(sp => sp.GetRequiredService<RecipeService>())
    .AddSingleton<ParserService>()
    .AddSingleton<FileRepository>()
    .AddSingleton<FileService>()
    .AddSingleton<ICacheKeyGetter>(sp => sp.GetRequiredService<FileService>())
    .AddSingleton<AdminService>()
    .AddHostedService<CacheRefreshBackgroundService>()
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
            if (error.Exception is GraphQLErrorException)
            {
                return error.WithMessage(error.Exception.Message).RemoveExtensions();
            }
            return error;
        })
        .AddAuthorization()
        .AddHttpRequestInterceptor<AuthInterceptor>()
        .AddQueryType()
        .AddMutationType()
            // Users
            .AddTypeExtension<UserQueries>()
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
            .AddTypeExtension<AdminMutations>()
        .AddType<UploadType>()
;


var app = builder.Build();

app.UseCors(o => o
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin());

app
    .UseWebSockets()
    .UseSerilogRequestLogging()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseEndpoints(endpoint =>
    {
        endpoint.MapGet("/", (ctx) =>
        {
            ctx.Response.Redirect("/graphql");
            return Task.CompletedTask;
        });
        endpoint.Map("/healthcheck", () => "OK");
        endpoint.MapGraphQL();
    });

try
{
    Log.Information("Starting API");
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
