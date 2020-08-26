using System;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet.Client.Options;
using NCoreUtils.AspNetCore;
using NCoreUtils.Queue.Internal;

namespace NCoreUtils.Queue.Mqtt
{
    public class Startup
    {
        private sealed class ConfigureJson : IConfigureOptions<JsonSerializerOptions>
        {
            public void Configure(JsonSerializerOptions options)
            {
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.Converters.Add(MediaQueueEntryConverter.Instance);
            }
        }

        static ForwardedHeadersOptions ConfigureForwardedHeaders()
        {
            var opts = new ForwardedHeadersOptions();
            opts.KnownNetworks.Clear();
            opts.KnownProxies.Clear();
            opts.ForwardedHeaders = ForwardedHeaders.All;
            return opts;
        }

        private readonly IConfiguration _configuration;

        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // MQTT client options
            var mqttConfig = _configuration.GetSection("Mqtt:Client").Get<MqttClientConfiguration>() ?? new MqttClientConfiguration();
            var mqttClientOptions =
                new MqttClientOptionsBuilder()
                    .WithTcpServer(mqttConfig.Host ?? throw new InvalidOperationException("No MQTT host supplied."), mqttConfig.Port)
                    .WithCleanSession(mqttConfig.CleanSession ?? true)
                    .Build();

            services
                // HTTP context
                .AddHttpContextAccessor()
                // JSON serialization
                .AddOptions<JsonSerializerOptions>().Services
                .ConfigureOptions<ConfigureJson>()
                .AddTransient(serviceProvider => serviceProvider.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>().CurrentValue)
                // MQTT client
                .AddSingleton<IMqttClientServiceOptions>(serviceProvider =>
                {
                    var options = ActivatorUtilities.CreateInstance<MqttClientServiceOptions>(serviceProvider);
                    _configuration.GetSection("Mqtt").Bind(options);
                    return options;
                })
                .AddSingleton(mqttClientOptions)
                .AddSingleton<IMqttClientService, MqttClientService>()
                .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<IMqttClientService>())
                // Queue implementation
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
                // Routing
                .AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
            #if DEBUG
            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            #endif

            app
                .UseForwardedHeaders(ConfigureForwardedHeaders())
                #if !DEBUG
                .UsePrePopulateLoggingContext()
                #endif
                .UseCors()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapProto<IMediaProcessingQueue>(MediaProcessingQueueProtoConfiguration.Configure);
                });
        }
    }
}
