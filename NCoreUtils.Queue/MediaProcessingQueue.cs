using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Queue
{
    public class MediaProcessingQueue : IMediaProcessingQueue
    {
        private readonly ILogger _logger;

        private readonly PublisherClient _publisherClient;

        private readonly JsonSerializerOptions _serializerOptions;

        public MediaProcessingQueue(ILogger<MediaProcessingQueue> logger, PublisherClient publisherClient, JsonSerializerOptions serializerOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publisherClient = publisherClient ?? throw new ArgumentNullException(nameof(publisherClient));
            _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        }

        public async Task EnqueueAsync(MediaQueueEntry entry, CancellationToken cancellationToken = default)
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(entry, _serializerOptions);
            var messageId = await _publisherClient.PublishAsync(ByteString.CopyFrom(data));
            _logger.LogInformation($"Successfully enqueued entry {entry} => {messageId}.");
        }
    }
}