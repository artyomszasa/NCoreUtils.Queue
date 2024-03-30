using System.Text.Json;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Queue;

internal static class SubscriberClientExtensions
{
    public static async Task RunAsync(this SubscriberClient subscriber, IServiceProvider services, CancellationToken cancellationToken)
    {
        using var __ = cancellationToken.Register(() =>
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
    }
}