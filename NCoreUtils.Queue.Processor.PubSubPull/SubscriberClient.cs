using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using NCoreUtils.Google.Cloud.PubSub;

namespace NCoreUtils.Queue;

internal partial class SubscriberClient(
    ILogger<SubscriberClient> logger,
    IPubSubV1Api pubSubClient,
    string projectId,
    string subscriptionId,
    ChannelReader<ReceivedMessage> producer,
    MediaEntryProcessor processor)
{
    public const int ConcurrentWorkerCount = 24;

    public ILogger<SubscriberClient> Logger { get; } = logger;

    public ChannelReader<ReceivedMessage> Producer { get; } = producer;

    public MediaEntryProcessor Processor { get; } = processor;

    /// <summary>
    ///
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the message should be acknowleged (i.e. message either has been successfully
    /// processed or is unprocessable)
    /// </returns>
    private async ValueTask<Result> ProcessSingleMessageAsync(
        PubSubMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Data is null)
        {
            return Result.Unprocessable("Message is empty");
        }
        try
        {
            MediaQueueEntry entry;
            try
            {
                entry = JsonSerializer.Deserialize(Convert.FromBase64String(message.Data), MediaProcessingQueueSerializerContext.Default.MediaQueueEntry)
                    ?? throw new InvalidOperationException("Unable to deserialize Pub/Sub request entry");
            }
            catch (Exception exn)
            {
                return Result.Unprocessable(exn);
            }
            var status = await Processor
                .ProcessAsync(entry, message.MessageId ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);
            return status < 400
                ? Result.Success
                : Result.Unprocessable("Implementation specific error occured");
        }
        catch (Exception exn)
        {
            return Result.Failure(exn);
        }
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var receivedMessage = await Producer.ReadAsync(cancellationToken).ConfigureAwait(false);
                var result = await ProcessSingleMessageAsync(receivedMessage.Message, cancellationToken).ConfigureAwait(false);
                if (VisitResult(result, receivedMessage.Message.MessageId))
                {
                    if (receivedMessage.AckId is string ackId)
                    {
                        try
                        {
                            await pubSubClient
                                .AcknowledgeAsync(projectId, subscriptionId, [ackId], CancellationToken.None)
                                .ConfigureAwait(false);
                            Logger.LogSubscriberClientAcknowlegeSuccess(ackId, receivedMessage.Message.MessageId);
                        }
                        catch (Exception exn)
                        {
                            Logger.LogSubscriberClientAcknowlegeFailed(exn, receivedMessage.Message.MessageId);
                        }
                    }
                    else
                    {
                        Logger.LogSubscriberClientNoAckId(receivedMessage.Message.MessageId);
                    }
                }
            }
            catch (OperationCanceledException) { /* noop */ }
            catch (Exception exn)
            {
                Logger.LogSubscriberClientWorkerFailed(exn);
            }
        }
    }
}