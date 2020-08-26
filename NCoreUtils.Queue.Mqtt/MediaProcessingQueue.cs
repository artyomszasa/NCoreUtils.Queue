using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Queue
{
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
            var messageId = await _publisherClient.PublishAsync(entry, cancellationToken);
            _logger.LogInformation($"Successfully enqueued entry {entry} => {messageId}.");
        }
    }
}