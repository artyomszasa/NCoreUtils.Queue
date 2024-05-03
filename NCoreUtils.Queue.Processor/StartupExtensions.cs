using System.Globalization;
using NCoreUtils.Logging;
using NCoreUtils.Logging.Google;

namespace NCoreUtils.Queue;

internal static class StartupExtensions
{
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

    public static TimeSpan GetTimeSpan(this IConfiguration configuration, string key, TimeSpan defaultValue)
    {
        var raw = configuration[key];
        if (!string.IsNullOrEmpty(raw))
        {
            if (!TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var value))
            {
                var fullPath = configuration is IConfigurationSection section
                    ? $"{section.Path}:{key}"
                    : key;
                throw new InvalidOperationException($"\"{raw}\" is not a valid TimeSpan at {fullPath}.");
            }
            return value;
        }
        return defaultValue;
    }

    public static IServiceCollection AddImagesHttpClientConfiguration(this IServiceCollection services, string configurationName, TimeSpan timeout)
    {
        services
            .AddHttpClient(configurationName)
                .ConfigureHttpClient(client => client.Timeout = timeout);
        return services;
    }

    public static IServiceCollection AddVideosHttpClientConfiguration(this IServiceCollection services, string configurationName, TimeSpan timeout)
    {
        services
            .AddHttpClient(configurationName)
                .ConfigureHttpClient(client => client.Timeout = timeout);
        return services;
    }
}