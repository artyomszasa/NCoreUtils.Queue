using NCoreUtils;
using NCoreUtils.AspNetCore;
using NCoreUtils.Images;
using NCoreUtils.Logging;
using NCoreUtils.Queue;

var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
{
    EnvironmentName = GetEnvironmentName(),
    ContentRootPath = Environment.CurrentDirectory
}).UsePortEnvironmentVariableToConfigureKestrel();
builder.Host.UseConsoleLifetime();

// CONFIGURATION *******************************************************************************************************
var configuration = new ConfigurationBuilder()
    .SetBasePath(Environment.CurrentDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("secrets/appsettings.json", optional: true, reloadOnChange: false)
    .Build();
builder.Configuration.AddConfiguration(configuration);

// LOGGING *************************************************************************************************************
builder.Logging.ConfigureGoogleLogging(builder.Environment, configuration);

// CONFIGURE ***********************************************************************************************************
const string ImagesClientConfiguration = nameof(ImagesClientConfiguration);
const string VideosClientConfiguration = nameof(VideosClientConfiguration);
builder.Services
    .AddHttpContextAccessor()
    // HTTP CLIENT
    .AddHttpClient()
    .AddImagesHttpClientConfiguration(ImagesClientConfiguration, configuration.GetTimeSpan("Timeouts:Images", TimeSpan.FromMinutes(15)))
    .AddVideosHttpClientConfiguration(VideosClientConfiguration, configuration.GetTimeSpan("Timeouts:Videos", TimeSpan.FromHours(2)))
    // GOOGLE
    .AddGoogleCloudStorageUtils()
    // Resources
    .AddCompositeResourceFactory(o => o
        .AddFileSystemResourceFactory()
        .AddGoogleCloudStorageResourceFactory(passthrough: true)
    )
    .AddImageResizerClient(
        endpoint: configuration.GetRequiredValue("Endpoints:Images"),
        allowInlineData: false,
        cacheCapabilities: true,
        httpClient: ImagesClientConfiguration
    )
    .AddVideoResizerClient(
        endpoint: configuration.GetRequiredValue("Endpoints:Videos"),
        allowInlineData: false,
        cacheCapabilities: true,
        httpClient: VideosClientConfiguration
    )
    // Media entry processor implementation
    .AddSingleton<MediaEntryProcessor>()
    // CORS
    .AddCors(b => b.AddDefaultPolicy(opts => opts
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        // must be at least 2 domains for CORS middleware to send Vary: Origin
        .WithOrigins("https://example.com", "http://127.0.0.1")
        .SetIsOriginAllowed(_ => true)
    ))
    .AddRouting();

// BUILD ***************************************************************************************************************
var app = builder.Build();

// POSTCONFIGURE *******************************************************************************************************
app
    // serving behind proxy
    .UseForwardedHeaders(configuration.GetSection("ForwardedHeaders"))
    // prepopulated logging context
    .UsePrePopulateLoggingContext()
    // health check
    .Use((context, next) =>
    {
        if (context.Request.Path == "/healthz")
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        }
        return next();
    })
    // CORS
    .UseCors()
    // logic
    .Run(context =>
    {
        var request = context.Request;
        if (request.Path == "/" && HttpMethods.IsPost(request.Method))
        {
            var processor = context.RequestServices.GetRequiredService<MediaEntryProcessor>();
            return processor.ProcessRequestAsync(context);
        }
        var response = context.Response;
        response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    });

// RUN *****************************************************************************************************************
app.Run();

static string GetEnvironmentName() => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
{
    null or "" => Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") switch
    {
        null or "" => "Development",
        string dotnetEnv => dotnetEnv
    },
    string aspNetCoreEnv => aspNetCoreEnv
};