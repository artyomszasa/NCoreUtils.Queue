using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using NCoreUtils.Logging;
using NCoreUtils.Logging.Google;

namespace NCoreUtils.Queue;

internal static class StartupExtensions
{
    internal static string GetRequiredValue(this IConfiguration configuration, string path)
    {
        var value = configuration[path];
        if (value is null)
        {
            var fullPath = configuration is IConfigurationSection section
                ? $"{section.Path}:{path}"
                : path;
            throw new InvalidOperationException($"No configuration value found at \"{fullPath}\".");
        }
        return value;
    }

    internal static int? GetOptionalInt32(this IConfiguration configuration, string path)
    {
        var value = configuration[path];
        if (value is null)
        {
            return default;
        }
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            var fullPath = configuration is IConfigurationSection section
                ? $"{section.Path}:{path}"
                : path;
            throw new FormatException($"Invalid integer value at \"{fullPath}\".");
        }
        return intValue;
    }

    internal static bool? GetOptionalBoolean(this IConfiguration configuration, string path)
    {
        var value = configuration[path];
        if (value is null)
        {
            return default;
        }
        if (!bool.TryParse(value, out var boolValue))
        {
            var fullPath = configuration is IConfigurationSection section
                ? $"{section.Path}:{path}"
                : path;
            throw new FormatException($"Invalid boolean value at \"{fullPath}\".");
        }
        return boolValue;
    }

    internal static MqttClientConfiguration GetMqttClientConfiguration(this IConfigurationSection configuration)
        => new(
            Host: configuration[nameof(MqttClientConfiguration.Host)],
            Port: configuration.GetOptionalInt32(nameof(MqttClientConfiguration.Port)),
            CleanSession: configuration.GetOptionalBoolean(nameof(MqttClientConfiguration.CleanSession))
        );

    internal static MqttClientServiceOptions GetMqttClientServiceOptions(this IConfigurationSection configuration)
        => new(
            Topic: configuration.GetRequiredValue(nameof(MqttClientServiceOptions.Topic))
        );

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