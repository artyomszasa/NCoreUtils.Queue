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
    private static string PrepareMessageData(MediaQueueEntry entry)
        => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(entry, MediaProcessingQueueSerializerContext.Default.MediaQueueEntry));

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly PublisherClient _publisherClient = publisherClient ?? throw new ArgumentNullException(nameof(publisherClient));

    public async Task EnqueueAsync(MediaQueueEntry entry, CancellationToken cancellationToken = default)
    {
        var data = PrepareMessageData(entry);
        var messageId = await _publisherClient.PublishAsync(data, cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogEnqueuedSuccessfully(entry, messageId);
        }
    }

    public async Task EnqueueMultipleAsync(IReadOnlyList<MediaQueueEntry> entries, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var data = entries.Select(PrepareMessageData);
        var messageIds = await _publisherClient.PublishAsync(data, cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            var index = 0;
            foreach (var entry in entries)
            {
                var messageId = messageIds.Count > index ? messageIds[index] : string.Empty;
                _logger.LogEnqueuedSuccessfully(entry, messageId);
                ++index;
            }
        }
    }
}