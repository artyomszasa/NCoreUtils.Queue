using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Queue.Proto;
using NCoreUtils.Proto;

namespace NCoreUtils.Queue;

[ProtoService(typeof(MediaProcessingQueueInfo), typeof(MediaProcessingQueueSerializerContext))]
public class MediaProcessingQueue : IMediaProcessingQueue
{
    private readonly ILogger _logger;

    private readonly IMqttClientService _publisherClient;

    public MediaProcessingQueue(ILogger<MediaProcessingQueue> logger, IMqttClientService publisherClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publisherClient = publisherClient ?? throw new ArgumentNullException(nameof(publisherClient));
    }

    public async Task EnqueueAsync(MediaQueueEntry entry, CancellationToken cancellationToken = default)
    {
        var messageId = await _publisherClient
            .PublishAsync(entry, MediaProcessingQueueSerializerContext.Default.MediaQueueEntry, cancellationToken)
            .ConfigureAwait(false);
        _logger.LogInformation("Successfully enqueued entry {Entry} => {MessageId}.", entry, messageId);
    }
}