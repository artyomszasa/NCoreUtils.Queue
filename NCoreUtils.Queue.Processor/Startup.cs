using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCoreUtils.Images;

namespace NCoreUtils.Queue.Processor
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
            services
                .AddImageResizerClient(_configuration.GetSection("Images"))
                .AddSingleton<MediaEntryProcessor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapPost("/", context =>
                    {
                        var processor = context.RequestServices.GetRequiredService<MediaEntryProcessor>();
                        return processor.ProcessRequestAsync(context);
                    });
                });
        }
    }
}
