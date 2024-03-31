using FirebaseAdmin;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using RecipesAPI.API;
using RecipesAPI.API.Auth;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Infrastructure;
using StackExchange.Redis;
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
using RecipesAPI.API.Features.Ratings.DAL;
using RecipesAPI.API.Features.Ratings.BLL;
using RecipesAPI.API.Features.Admin.DAL;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Serilog;
using System.Text;
using Serilog.Sinks.Slack;
using Serilog.Sinks.Slack.Models;
using Google.Cloud.Storage.V1;
using FirebaseAdmin.Auth;
using RecipesAPI.API.HotChocolateHelpers;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Debug()
    .Enrich.FromLogContext();
if (!builder.Environment.IsDevelopment())
{
    loggerConfig = loggerConfig
    .WriteTo.Console(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter())
    .WriteTo.Slack(new SlackSinkOptions
    {
        WebHookUrl = builder.Configuration["SLACK_WEBHOOK_URL"],
        MinimumLogEventLevel = Serilog.Events.LogEventLevel.Error,
        CustomUserName = "Slack Logger",
        ShowExceptionAttachments = true,
    });
}
else
{
    loggerConfig = loggerConfig
    .WriteTo.Console();

}
Log.Logger = loggerConfig.CreateLogger();
builder.Host.UseSerilog(Log.Logger);


var googleAppCredContent = builder.Configuration["GOOGLE_APPLICATION_CREDENTIALS_CONTENT"];
if (!string.IsNullOrEmpty(googleAppCredContent))
{
    await File.WriteAllTextAsync("/tmp/serviceaccount.json", googleAppCredContent);
    System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "/tmp/serviceaccount.json");
}

var port = 5001;
if (int.TryParse(builder.Configuration["PORT"], out var _port)) port = _port;

FirebaseApp.Create();

var jwtUtil = new JwtUtil(builder.Configuration["FirebaseAppId"]!, null);

builder.Services
    .AddDependencies(builder.Configuration, builder.Environment)
    .AddHttpClient()
    .AddControllers().AddControllersAsServices().Services
    .AddSwaggerGen()
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
    .AddHostedService<CacheRefreshBackgroundService>()
    .AddHostedService<HangfireRecurringJobs>()
    .AddHttpContextAccessor()
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
        .AddInstrumentation(o =>
        {
            o.RenameRootActivity = true;
            o.IncludeDocument = true;
        })
        .AddHttpRequestInterceptor<AuthInterceptor>()
        .AddRecipesAPITypes()
;


builder.WebHost
    .UseKestrel(serverOptions => serverOptions.ListenAnyIP(port));

var otlpExporterHttpClient = new HttpClient(new HttpClientInterceptor());
otlpExporterHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{builder.Configuration["GRAFANA_CLOUD_OTEL_INSTANCE_ID"]}:{builder.Configuration["GRAFANA_CLOUD_OTEL_KEY"]}")));
var httpClientFactory = () =>
{
    return otlpExporterHttpClient;
};
builder.Services.AddOpenTelemetry()
    .WithTracing(tracingProviderBuilder =>
        tracingProviderBuilder
            .AddSource(Telemetry.ActivitySource.Name)
            .ConfigureResource(resource => resource.AddService(Telemetry.ServiceName))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddGrpcClientInstrumentation()
            .AddSqlClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnableConnectionLevelAttributes = true;
                options.SetDbStatementForText = true;
                options.SetDbStatementForStoredProcedure = true;
            })
            .AddRedisInstrumentation()
            .AddHotChocolateInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                options.Endpoint = new Uri(builder.Configuration["GRAFANA_CLOUD_OTEL_ENDPOINT"] ?? throw new Exception("missing GRAFANA_CLOUD_OTEL_ENDPOINT"));
            })
            .SetSampler(new AlwaysOnSampler())
    );


var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseCors(o => o
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
    .UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/hangfire"), hangfireApp => hangfireApp.UseHangfireAuthorizationMiddleware())
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
    Log.Information("LocalData: {connStr}", app.Configuration.GetConnectionString("LocalData"));
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SqliteDataContext>();
    await context.Init();
}
catch (Exception ex)
{
    Log.Error(ex, "failed to initialise sqlite");
}

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
