using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils;
using NCoreUtils.Google;
using NCoreUtils.Google.Cloud.Monitoring;
using NCoreUtils.Google.Cloud.PubSub;
using NCoreUtils.Images;
using NCoreUtils.Logging;
using NCoreUtils.Queue;

internal class Program
{
    private static async Task Main(string[] args)
    {
        const string ImagesHttpClientConfiguration = "images";
        const string VideosHttpClientConfiguration = "videos";

        var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };
        using var _ = System.Runtime.InteropServices.PosixSignalRegistration.Create(System.Runtime.InteropServices.PosixSignal.SIGTERM, ctx =>
        {
            ctx.Cancel = true;
            cancellation.Cancel();
        });

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("secrets/appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddHttpClient(VideosHttpClientConfiguration)
                .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(120));

        serviceCollection
            .AddHttpClient(ImagesHttpClientConfiguration)
                .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(15));

        var googleCredentials = await ServiceAccountCredentialData.ReadDefaultAsync(CancellationToken.None);

        // CONFIGURE ***************************************************************************************************
        using var services = serviceCollection
            // LOGGING
            .AddLogging(b => b
                .ClearProviders()
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddGoogleFluentd(projectId: configuration["Google:ProjectId"])
            )
            // HTTP CLIENT
            .ConfigureHttpClientDefaults(b =>
            {
                b.UseSocketsHttpHandler(configureHandler: (opts, _) =>
                {
                    opts.PooledConnectionLifetime = TimeSpan.Zero;
                    opts.PooledConnectionIdleTimeout = TimeSpan.Zero;
                });
            })
            // GOOGLE (STORAGE)
            .AddGoogleCloudStorageUtils(googleCredentials)
            // GOOGLE (PUB/SUB)
            .AddGoogleCloudPubSubClient(googleCredentials)
            // GOOGLE (MONITORING)
            .AddGoogleCloudMonitoringClient(googleCredentials)
            // Resources
            .AddCompositeResourceFactory(o => o
                .AddFileSystemResourceFactory()
                .AddGoogleCloudStorageResourceFactory(passthrough: false)
            )
            // Images client
            .AddImageResizerClient(
                endpoint: configuration.GetRequiredValue("Endpoints:Images"),
                allowInlineData: false,
                cacheCapabilities: true,
                httpClient: ImagesHttpClientConfiguration
            )
            // Videos client
            .AddVideoResizerClient(endpoint: configuration.GetRequiredValue("Endpoints:Videos"),
                allowInlineData: false,
                cacheCapabilities: true,
                httpClient: VideosHttpClientConfiguration)
            // Processor implementation
            .AddSingleton<MediaEntryProcessor>()
            .BuildServiceProvider(true);

        var projectId = configuration.GetRequiredValue("Google:ProjectId");
        var subscriptionId = configuration.GetRequiredValue("Google:SubscriptionId");
        var pubSubClient = services.GetRequiredService<IPubSubV1Api>();
        var processor = services.GetRequiredService<MediaEntryProcessor>();
        var logger = services.GetRequiredService<ILogger<SubscriberClient>>();

        // METRICS *****************************************************************************************************
        var monitoredResource = await ScheduledHeapMetricsDispatcher.FetchCurrentResourceDataAsync(
            httpClientFactory: services.GetRequiredService<IHttpClientFactory>(),
            cancellationToken: cancellation.Token
        );
        var metricsTask = new ScheduledHeapMetricsDispatcher(
            logger: services.GetRequiredService<ILogger<ScheduledHeapMetricsDispatcher>>(),
            api: services.GetRequiredService<IMonitoringV3Api>(),
            projectId: projectId,
            resource: monitoredResource,
            delay: TimeSpan.FromSeconds(20)
        ).RunAsync(cancellation.Token);

        // PROCESSIG ***************************************************************************************************
        var channel = Channel.CreateUnbounded<ReceivedMessage>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });

        // WORKER TASKS
        var workerTask = Task.WhenAll(Enumerable.Range(0, SubscriberClient.ConcurrentWorkerCount).Select(_ =>
        {
            return new SubscriberClient(
                logger,
                pubSubClient,
                projectId,
                subscriptionId,
                channel.Reader,
                processor
            ).ProcessAsync(cancellation.Token);
        }));

        logger.LogSubscriberClientMessageProcessStarted();

        // MAIN LOOP
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                var messages = await pubSubClient.PullAsync(projectId, subscriptionId, 12, cancellation.Token);
                if (messages.ReceivedMessages is { Count: > 0 } receivedMessages)
                {
                    logger.LogSubscriberClientReceivedMessages(receivedMessages.Count);
                    foreach (var message in receivedMessages)
                    {
                        await channel.Writer.WriteAsync(message, cancellation.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) { /* noop */ }
            catch (Exception exn)
            {
                logger.LogSubscriberClientPullFailed(exn);
            }
        }

        // await worker completion
        await workerTask.ConfigureAwait(false);
        // await metric dispatcher completion
        await metricsTask.ConfigureAwait(false);
        // log completion
        logger.LogSubscriberClientMessageProcessStoppedSuccessfully();
    }
}