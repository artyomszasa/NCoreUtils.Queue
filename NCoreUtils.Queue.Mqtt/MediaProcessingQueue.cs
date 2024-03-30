using NCoreUtils.Queue.Proto;
using NCoreUtils.Proto;
using System.Text.Json.Serialization;

namespace NCoreUtils.Queue;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JsonRootMediaProcessingQueueInfo))]
internal partial class MediaProcessingQueueSerializerContext : JsonSerializerContext { }

[ProtoService(typeof(MediaProcessingQueueInfo), typeof(MediaProcessingQueueSerializerContext))]
public partial class MediaProcessingQueue(ILogger<MediaProcessingQueue> logger, IMqttClientService publisherClient)
    : IMediaProcessingQueue
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMqttClientService _publisherClient = publisherClient ?? throw new ArgumentNullException(nameof(publisherClient));

    public async Task EnqueueAsync(MediaQueueEntry entry, CancellationToken cancellationToken = default)
    {
        var messageId = await _publisherClient
            .PublishAsync(entry, MediaProcessingQueueSerializerContext.Default.MediaQueueEntry, cancellationToken)
            .ConfigureAwait(false);
        _logger.LogInformation("Successfully enqueued entry {Entry} => {MessageId}.", entry, messageId);
    }
}