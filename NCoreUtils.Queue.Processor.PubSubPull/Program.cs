using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils;
using NCoreUtils.Images;
using NCoreUtils.Logging;
using NCoreUtils.Queue;

internal class Program
{
    // NOTE: Required to use GOOGLE_APPLICATION_CREDENTIALS
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Google.Apis.Auth.OAuth2.JsonCredentialParameters))]
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

        // CONFIGURE ***************************************************************************************************
        using var services = serviceCollection
            // LOGGING
            .AddLogging(b => b
                .ClearProviders()
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddGoogleFluentd(projectId: configuration["Google:ProjectId"])
            )
            // HTTP CLIENT
            .AddHttpClient()
            // GOOGLE
            .AddGoogleCloudStorageUtils()
            // Resources
            .AddCompositeResourceFactory(o => o
                .AddFileSystemResourceFactory()
                .AddGoogleCloudStorageResourceFactory(passthrough: true)
            )
            // IMAGES
            .AddImageResizerClient(
                endpoint: configuration.GetRequiredValue("Endpoints:Images"),
                allowInlineData: false,
                cacheCapabilities: true,
                httpClient: ImagesHttpClientConfiguration
            )
            // VIDEOS
            .AddVideoResizerClient(endpoint: configuration.GetRequiredValue("Endpoints:Videos"),
                allowInlineData: false,
                cacheCapabilities: true,
                httpClient: VideosHttpClientConfiguration)
            // Processor implementation
            .AddSingleton<MediaEntryProcessor>()
            .BuildServiceProvider(true);

        var subscriptionName = SubscriptionName.FromProjectSubscription(
            projectId: configuration.GetRequiredValue("Google:ProjectId"),
            subscriptionId: configuration.GetRequiredValue("Google:SubscriptionId")
        );
        var subscriber = await new SubscriberClientBuilder
        {
            SubscriptionName = subscriptionName,
            Logger = services.GetRequiredService<ILogger<SubscriberClient>>(),
            Settings = new SubscriberClient.Settings
            {
                AckDeadline = TimeSpan.FromMinutes(5)
            },
            GrpcAdapter = Google.Api.Gax.Grpc.GrpcNetClientAdapter.Default
        }.BuildAsync(cancellation.Token).ConfigureAwait(false);
        await subscriber.RunAsync(services, cancellation.Token).ConfigureAwait(false);
    }
}