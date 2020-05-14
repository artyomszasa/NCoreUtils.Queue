using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCoreUtils.AspNetCore;
using NCoreUtils.Data;
using NCoreUtils.Queue.Internal;

namespace NCoreUtils.Queue
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
                .AddSingleton(Data.QueueModel.CreateBuilder())
                .AddFirestoreDataRepositoryContext(new FirestoreConfiguration { ProjectId = _configuration["Google:ProjectId"] })
                .AddFirestoreDataRepository<Data.Entry>()
                .AddScoped<IMediaProcessingQueue, MediaProcessingQueue>()
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
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapProto<IMediaProcessingQueue>(MediaProcessingQueueProtoConfiguration.Configure);
                });
        }
    }
}
