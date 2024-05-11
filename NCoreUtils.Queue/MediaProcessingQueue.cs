using System.Text.Json;
using Google.Protobuf;
using NCoreUtils.Queue.Proto;
using NCoreUtils.Proto;
using System.Text.Json.Serialization;

namespace NCoreUtils.Queue;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JsonRootMediaProcessingQueueInfo))]
internal partial class MediaProcessingQueueSerializerContext : JsonSerializerContext { }

[ProtoService(typeof(MediaProcessingQueueInfo), typeof(MediaProcessingQueueSerializerContext))]
public partial class MediaProcessingQueue(ILogger<MediaProcessingQueue> logger, PublisherClient publisherClient) : IMediaProcessingQueue
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly PublisherClient _publisherClient = publisherClient ?? throw new ArgumentNullException(nameof(publisherClient));

    public async Task EnqueueAsync(MediaQueueEntry entry, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(entry, MediaProcessingQueueSerializerContext.Default.MediaQueueEntry);
        var messageId = await _publisherClient.PublishAsync(Convert.ToBase64String(data), cancellationToken).ConfigureAwait(false);
        _logger.LogEnqueuedSuccessfully(entry, messageId);
    }
}