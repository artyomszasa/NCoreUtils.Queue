using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using NCoreUtils.Queue.Proto;
using NCoreUtils.Proto;

namespace NCoreUtils.Queue;

[ProtoService(typeof(MediaProcessingQueueInfo), typeof(MediaProcessingQueueSerializerContext))]
public class MediaProcessingQueue : IMediaProcessingQueue
{
    private readonly ILogger _logger;

    private readonly PublisherClient _publisherClient;

    public MediaProcessingQueue(ILogger<MediaProcessingQueue> logger, PublisherClient publisherClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publisherClient = publisherClient ?? throw new ArgumentNullException(nameof(publisherClient));
    }

    public async Task EnqueueAsync(MediaQueueEntry entry, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(entry, MediaProcessingQueueSerializerContext.Default.MediaQueueEntry);
        var messageId = await _publisherClient.PublishAsync(ByteString.CopyFrom(data));
        _logger.LogInformation("Successfully enqueued entry {Entry} => {MessageId}.", entry, messageId);
    }
}