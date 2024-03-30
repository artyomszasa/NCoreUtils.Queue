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