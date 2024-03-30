using NCoreUtils.AspNetCore;
using NCoreUtils.Logging;
using NCoreUtils.Queue;

var builder = WebApplication.CreateBuilder(args).UsePortEnvironmentVariableToConfigureKestrel();

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
builder.Services
    // HTTP CONTEXT accessor
    .AddHttpContextAccessor()
    // PUB/SUB publisher
    .AddPubSubPublisherClient(configuration)
    // Media processing queue implementation
    .AddSingleton<IMediaProcessingQueue, MediaProcessingQueue>()
    // CORS
    .AddCors(b => b.AddDefaultPolicy(opts => opts
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        // must be at least 2 domains for CORS middleware to send Vary: Origin
        .WithOrigins("https://example.com", "http://127.0.0.1")
        .SetIsOriginAllowed(_ => true)
    ))
    // ROUTING
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
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        }
        return next();
    })
    // CORS
    .UseCors()
    // routing
    .UseRouting()
    // endpoints
    .UseEndpoints(endpoints =>
    {
        endpoints.MapMediaProcessingQueue();
    });

// RUN *****************************************************************************************************************
app.Run();