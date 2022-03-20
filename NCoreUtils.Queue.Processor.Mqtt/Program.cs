using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client.Options;
using NCoreUtils.Images;
using NCoreUtils.Queue.Internal;

namespace NCoreUtils.Queue
{
    class Program
    {
        private sealed class ConfigureJson : IConfigureOptions<JsonSerializerOptions>
        {
            public void Configure(JsonSerializerOptions options)
            {
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.Converters.Add(MediaQueueEntryConverter.Instance);
            }
        }

        private static IConfiguration CreateConfiguration()
            => new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("secrets/appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "MqttClientConfiguration preserved explicitely")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MqttClientConfiguration))]
        private static MqttClientConfiguration GetMqttClientConfiguration(IConfiguration configuration)
            => configuration.GetSection("Mqtt:Client").Get<MqttClientConfiguration>() ?? new MqttClientConfiguration();

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "MqttClientConfiguration preserved explicitely")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MqttClientServiceOptions))]
        private static void BindMqttClientServiceOptions(IConfiguration configuration, MqttClientServiceOptions options)
            => configuration.GetSection("Mqtt").Bind(options);

#pragma warning disable IDE0060
        public static IHostBuilder CreateHostBuilder(string[] args)
#pragma warning restore IDE0060
        {
            var configuration = CreateConfiguration();
            // MQTT client options
            var mqttConfig = GetMqttClientConfiguration(configuration);
            var mqttClientOptions =
                new MqttClientOptionsBuilder()
                    .WithTcpServer(mqttConfig.Host ?? throw new InvalidOperationException("No MQTT host supplied."), mqttConfig.Port)
                    .WithCleanSession(mqttConfig.CleanSession ?? true)
                    .WithClientId(mqttConfig.ClientId ?? "ncoreutils-queue-client")
                    .Build();
            return new HostBuilder()
                .UseConsoleLifetime()
                .UseContentRoot(Environment.CurrentDirectory)
                .UseEnvironment(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development")
                .ConfigureLogging(b => b
                    .ClearProviders()
                    .AddConfiguration(configuration.GetSection("Logging"))
                    .AddConsole()
                )
                .ConfigureServices((context, services) =>
                {
                    services
                        // HTTP client
                        .AddHttpClient()
                        // JSON serialization
                        .AddOptions<JsonSerializerOptions>().Services
                        .ConfigureOptions<ConfigureJson>()
                        .AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().CurrentValue)
                        // MQTT client
                        .AddSingleton<IMqttClientServiceOptions>(serviceProvider =>
                        {
                            var options = ActivatorUtilities.CreateInstance<MqttClientServiceOptions>(serviceProvider);
                            BindMqttClientServiceOptions(configuration, options);
                            return options;
                        })
                        .AddSingleton(mqttClientOptions)
                        .AddHostedService<MqttSubscriberService>()
                        .AddImageResizerClient(configuration.GetSection("Images"))
                        .AddVideoResizerClient(configuration.GetSection("Videos"))
                        .AddSingleton<MediaEntryProcessor>();
                });
        }
    }
}
