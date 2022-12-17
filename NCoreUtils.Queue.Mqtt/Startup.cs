using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Client;
using NCoreUtils.AspNetCore;

namespace NCoreUtils.Queue.Mqtt
{
    public class Startup
    {
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
            var mqttConfig = _configuration.GetRequiredSection("Mqtt:Client").GetMqttClientConfiguration();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(mqttConfig.Host ?? throw new InvalidOperationException("No MQTT host supplied."), mqttConfig.Port)
                .WithCleanSession(mqttConfig.CleanSession ?? true)
                .Build();

            services
                // HTTP context
                .AddHttpContextAccessor()
                // MQTT client
                .AddSingleton<IMqttClientServiceOptions>(_configuration.GetRequiredSection("Mqtt").GetMqttClientServiceOptions())
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
                .UseForwardedHeaders(_configuration.GetSection("ForwardedHeaders"))
#if !DEBUG
                .UsePrePopulateLoggingContext()
#endif
                .UseCors()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapMediaProcessingQueue();
                });
        }
    }
}
