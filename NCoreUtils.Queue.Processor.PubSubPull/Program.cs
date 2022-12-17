﻿using System;
using System.Text.Json;
using System.Threading;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils;
using NCoreUtils.Images;
using NCoreUtils.Logging;
using NCoreUtils.Queue;

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

using var services = new ServiceCollection()
    .AddLogging(b => b
        .ClearProviders()
        .AddConfiguration(configuration.GetSection("Logging"))
        .AddGoogleFluentd(projectId: configuration["Google:ProjectId"])
    )
    .AddHttpClient()
    .AddGoogleCloudStorageUtils()
    .AddImageResizerClient(
        endpoint: configuration.GetRequiredValue("Endpoints:Images"),
        allowInlineData: false,
        cacheCapabilities: true
    )
    .AddSingleton<MediaEntryProcessor>()
    .BuildServiceProvider(true);

var subscriptionName = SubscriptionName.FromProjectSubscription(
    projectId: configuration.GetRequiredValue("Google:ProjectId"),
    subscriptionId: configuration.GetRequiredValue("Google:SubscriptionId")
);
var subscriber = await new SubscriberClientBuilder
{
    Logger = services.GetRequiredService<ILogger<SubscriberClient>>(),
    Settings = new SubscriberClient.Settings
    {
        AckDeadline = TimeSpan.FromMinutes(5)
    }
}.BuildAsync(cancellation.Token).ConfigureAwait(false);
using var __ = cancellation.Token.Register(() =>
{
    _ = subscriber.StopAsync(TimeSpan.FromSeconds(5));
});
var processor = services.GetRequiredService<MediaEntryProcessor>();
processor.Logger.LogDebug("Start processing messages.");
await subscriber.StartAsync(async (message, cancellationToken) =>
{
    var messageId = message.MessageId;
    processor.Logger.LogDebug("Processing message {MessageId}.", messageId);
    try
    {
        MediaQueueEntry entry;
        try
        {
            entry = JsonSerializer.Deserialize(message.Data.ToByteArray(), MediaProcessingQueueSerializerContext.Default.MediaQueueEntry)
                ?? throw new InvalidOperationException("Unable to deserialize Pub/Sub request entry.");
        }
        catch (Exception exn)
        {
            processor.Logger.LogError(exn, "Failed to deserialize pub/sub message {MessageId}.", messageId);
            return SubscriberClient.Reply.Ack; // Message should not be retried...
        }
        var status = await processor.ProcessAsync(entry, messageId, cancellationToken).ConfigureAwait(false);
        var ack = status < 400 ? SubscriberClient.Reply.Ack : SubscriberClient.Reply.Nack;
        processor.Logger.LogDebug("Processed message {MessageId} => {Ack}.", messageId, ack);
        return ack;
    }
    catch (Exception exn)
    {
        processor.Logger.LogError(exn, "Failed to process pub/sub message {MessageId}.", messageId);
        return SubscriberClient.Reply.Nack;
    }
}).ConfigureAwait(false);
processor.Logger.LogDebug("Processing messages stopped succefully.");
