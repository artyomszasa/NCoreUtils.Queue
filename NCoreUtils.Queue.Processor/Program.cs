using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;
using NCoreUtils.Images;

namespace NCoreUtils.Queue.Processor
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("secrets/appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddLogging(b => b.ClearProviders().SetMinimumLevel(LogLevel.Trace).AddConsole())
                        .AddSingleton(Data.QueueModel.CreateBuilder())
                        .AddImageResizerClient(configuration.GetSection("Images"))
                        .AddFirestoreDataRepositoryContext(new FirestoreConfiguration { ProjectId = configuration["Google:ProjectId"] })
                        .AddFirestoreDataRepository<Data.Entry>()
                        .AddHostedService<ImageProcessor>();
                });
        }
    }
}
