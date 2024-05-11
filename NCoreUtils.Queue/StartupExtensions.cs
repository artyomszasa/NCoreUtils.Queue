using System.Globalization;
using NCoreUtils.Google.Cloud.PubSub;
using NCoreUtils.Logging;
using NCoreUtils.Logging.Google;

namespace NCoreUtils.Queue;

internal static class StartupExtensions
{
    private static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            var path = configuration is IConfigurationSection section ? $"{section.Path}:{key}" : key;
            throw new InvalidOperationException($"No required value found at {path}");
        }
        return value;
    }

    public static ILoggingBuilder ConfigureGoogleLogging(
        this ILoggingBuilder builder,
        IHostEnvironment env,
        IConfiguration configuration)
    {
        builder
            .ClearProviders()
            .AddConfiguration(configuration.GetSection("Logging"));
        builder.Services
            .AddDefaultTraceIdProvider();
#if DEBUG
        if (env.IsDevelopment())
        {
            builder.AddConsole().AddDebug();
        }
        else
        {
#endif
            builder.Services
                .AddLoggingContext()
                .AddSingleton<ILabelProvider>(new AspNetCoreConnectionIdLabelProvider());
            builder.AddGoogleFluentd<AspNetCoreLoggerProvider>(projectId: configuration["Google:ProjectId"]);
#if DEBUG
        }
#endif
        return builder;
    }

    public static IServiceCollection AddPubSubPublisherClient(this IServiceCollection services, IConfiguration configuration)
    {
        var projectId = configuration.GetRequiredValue("Google:ProjectId");
        var topic = configuration.GetRequiredValue("Google:TopicId");
        return services
            .AddGoogleCloudPubSubClient()
            .AddSingleton<PublisherClient>(serviceProvider => new(
                projectId: projectId,
                topic: topic,
                api: serviceProvider.GetRequiredService<IPubSubV1Api>()
            ));
    }

    public static WebApplicationBuilder UsePortEnvironmentVariableToConfigureKestrel(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseKestrelCore();
        if (Environment.GetEnvironmentVariable("PORT") is string rawPort)
        {
            if (!int.TryParse(rawPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
            {
                throw new InvalidOperationException("\"{rawPort}\" is not a valid port to listen to.");
            }
            builder.WebHost.ConfigureKestrel(o =>
            {
                o.ListenAnyIP(port);
            });
        }
        return builder;
    }
}