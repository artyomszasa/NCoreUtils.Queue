using System;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCoreUtils.AspNetCore;
using NCoreUtils.Queue.Internal;

namespace NCoreUtils.Queue
{
    public class Startup
    {
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
            var publisherTask = PublisherClient.CreateAsync(new TopicName(_configuration["Google:ProjectId"], _configuration["Google:TopicId"]));
            services
                .AddHttpContextAccessor()
                .AddSingleton(_ => publisherTask.Result)
                .AddSingleton<IMediaProcessingQueue, MediaProcessingQueue>()
                .AddCors(b => b.AddDefaultPolicy(opts => opts
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    // must be at least 2 domains for CORS middleware to send Vary: Origin
                    .WithOrigins("https://example.com", "http://127.0.0.1")
                    .SetIsOriginAllowed(_ => true)
                ))
                .AddSingleton(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { MediaQueueEntryConverter.Instance }
                })
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
                .Use((context, next) =>
                {
                    if (context.Request.Path == "/healthz")
                    {
                        context.Response.StatusCode = 200;
                        return Task.CompletedTask;
                    }
                    return next();
                })
                .UseCors()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapProto<IMediaProcessingQueue>(MediaProcessingQueueProtoConfiguration.Configure);
                });
        }
    }
}
