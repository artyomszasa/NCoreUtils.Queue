using NCoreUtils;
using NCoreUtils.AspNetCore;
using NCoreUtils.Images;
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
    .AddHttpContextAccessor()
        // HTTP CLIENT
        .AddHttpClient()
        // GOOGLE
        .AddGoogleCloudStorageUtils()
        // Resources
        .AddCompositeResourceFactory(o => o
            .AddFileSystemResourceFactory()
            .AddGoogleCloudStorageResourceFactory(passthrough: true)
        )
        .AddImageResizerClient(configuration.GetSection("Images"))
        // .AddVideoResizerClient(_configuration.GetSection("Videos"))
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
        endpoints.MapPost("/", context =>
        {
            var processor = context.RequestServices.GetRequiredService<MediaEntryProcessor>();
            return processor.ProcessRequestAsync(context);
        });
    });

// RUN *****************************************************************************************************************
app.Run();