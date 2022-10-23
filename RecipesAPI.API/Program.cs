using FirebaseAdmin;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using RecipesAPI.API;
using RecipesAPI.Auth;
using RecipesAPI.Infrastructure;
using RecipesAPI.Recipes.BLL;
using RecipesAPI.Recipes.DAL;
using RecipesAPI.Recipes.Graph;
using RecipesAPI.Users;
using RecipesAPI.Users.BLL;
using RecipesAPI.Users.DAL;
using RecipesAPI.Users.Graph;
using StackExchange.Redis;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var port = 5000;
if (int.TryParse(builder.Configuration["PORT"], out var _port)) port = _port;

builder.WebHost.UseKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(port);
});

builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}
else
{
    builder.Logging.AddJsonConsole();
}

FirebaseApp.Create();

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
        return new CacheProvider(distributedCache, keyPrefix);
    })
    .AddSingleton(sp =>
    {
        return FirestoreDb.Create(builder.Configuration["FirebaseAppId"]);
    })
    .AddSingleton(sp =>
    {
        var db = sp.GetRequiredService<FirestoreDb>();
        return new UserRepository(builder.Configuration["FirebaseWebApiKey"], db);
    })
    .AddSingleton<UserService>()
    .AddSingleton<RecipeRepository>()
    .AddSingleton<RecipeService>()
    .AddStackExchangeRedisCache(options =>
    {
        var endpointCollection = new EndPointCollection();
        options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
        {
            User = builder.Configuration["REDIS_USER"],
            Password = builder.Configuration["REDIS_PASSWORD"],
            ClientName = "RecipesApi",
        };
        options.ConfigurationOptions.EndPoints.Add($"{builder.Configuration["REDIS_HOST"]}:{builder.Configuration["REDIS_PORT"]}");
    })
    .AddDistributedMemoryCache()
    .AddCors()
    .AddGraphQLServer()
        .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment())
        .AddDiagnosticEventListener<ErrorLoggingDiagnosticsEventListener>()
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
        .AddType<UploadType>()
;


var app = builder.Build();

app.UseCors(o => o
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin());

app
    .UseWebSockets()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseEndpoints(endpoint => endpoint.MapGraphQL());

app.Run();
