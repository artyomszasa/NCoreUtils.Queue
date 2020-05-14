using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images;
using NCoreUtils.Queue.Internal;

namespace NCoreUtils.Queue
{
    public class MediaEntryProcessor
    {
        static private readonly JsonSerializerOptions requestSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static private readonly JsonSerializerOptions entrySerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { MediaQueueEntryConverter.Instance }
        };

        private ILogger _logger;

        private readonly IImageResizer _resizer;

        public MediaEntryProcessor(ILogger<MediaEntryProcessor> logger, IImageResizer resizer)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _resizer = resizer ?? throw new System.ArgumentNullException(nameof(resizer));
        }

        public async Task ProcessRequestAsync(HttpContext context)
        {
            PubSubRequest req;
            MediaQueueEntry entry;
            try
            {
                req = await JsonSerializer.DeserializeAsync<PubSubRequest>(context.Request.Body, requestSerializerOptions, context.RequestAborted);
                entry = JsonSerializer.Deserialize<MediaQueueEntry>(Convert.FromBase64String(req.Message.Data), entrySerializerOptions);
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, "Failed to deserializer pub/sub request message.");
                context.Response.StatusCode = 204; // Message should not be retried...
                return;
            }
            context.Response.StatusCode = await ProcessAsync(entry, req.Message.MessageId, context.RequestAborted);
            return;
        }

        public async Task<int> ProcessAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(entry.EntryType))
            {
                _logger.LogError($"Failed to process entry: missing entry type. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (entry.EntryType != MediaQueueEntryTypes.Image)
            {
                _logger.LogError($"Failed to process entry: unsupported entry type = {entry.EntryType}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (Uri.TryCreate(entry.Source, UriKind.Absolute, out var sourceUri))
            {
                _logger.LogError($"Failed to process entry: missing or invalid source uri = {entry.Source}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (Uri.TryCreate(entry.Target, UriKind.Absolute, out var targetUri))
            {
                _logger.LogError($"Failed to process entry: missing or invalid target uri = {entry.Target}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (sourceUri.Scheme != "gs")
            {
                _logger.LogError($"Failed to process entry: unsupported source uri = {entry.Source}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (targetUri.Scheme != "gs")
            {
                _logger.LogError($"Failed to process entry: unsupported target uri = {entry.Target}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            try
            {
                await _resizer.ResizeAsync(
                    new GoogleCloudStorageSource(sourceUri),
                    new GoogleCloudStorageDestination(targetUri),
                    new ResizeOptions(
                        entry.TargetType,
                        entry.TargetWidth,
                        entry.TargetHeight,
                        entry.Operation,
                        weightX: entry.WeightX,
                        weightY: entry.WeightY
                    ),
                    cancellationToken
                );
            }
            catch (InvalidImageException exn)
            {
                _logger.LogError(exn, $"Failed to process image entry. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            catch (UnsupportedImageTypeException exn)
            {
                _logger.LogError(exn, $"Failed to process image entry. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            catch (UnsupportedResizeModeException exn)
            {
                _logger.LogError(exn, $"Failed to process image entry. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, $"Failed to process image entry, operation may be retried. [messageId = {messageId}]");
                return 400; // Message should not be retried...
            }
            return 200;
        }
    }
}