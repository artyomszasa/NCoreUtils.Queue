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

        private readonly IImageResizer _imageResizer;

        private readonly IVideoResizer _videoResizer;

        public MediaEntryProcessor(ILogger<MediaEntryProcessor> logger, IImageResizer imageResizer, IVideoResizer videoResizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageResizer = imageResizer ?? throw new ArgumentNullException(nameof(imageResizer));
            _videoResizer = videoResizer ?? throw new ArgumentNullException(nameof(videoResizer));
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

        public async Task<int> ProcessImageAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(entry.Source, UriKind.Absolute, out var sourceUri))
            {
                _logger.LogError($"Failed to process entry: missing or invalid source uri = {entry.Source}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (!Uri.TryCreate(entry.Target, UriKind.Absolute, out var targetUri))
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
                await _imageResizer.ResizeAsync(
                    new GoogleCloudStorageSource(sourceUri),
                    new GoogleCloudStorageDestination(targetUri, isPublic: true, cacheControl: "private, max-age=31536000"),
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
                return 400; // Message should be retried...
            }
            return 200;
        }

        public async Task<int> ProcessVideoAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(entry.Source, UriKind.Absolute, out var sourceUri))
            {
                _logger.LogError($"Failed to process entry: missing or invalid source uri = {entry.Source}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (!Uri.TryCreate(entry.Target, UriKind.Absolute, out var targetUri))
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
            if (string.IsNullOrEmpty(entry.Operation) || entry.Operation == "resize")
            {
                try
                {
                    await _videoResizer.ResizeAsync(sourceUri, targetUri, new Videos.VideoOptions(entry.TargetType ?? "mp4", entry.TargetWidth, entry.TargetHeight, 75), cancellationToken);
                    _logger.LogInformation("Successfully processed video {0} => {1}.", entry.Source, entry.Target);
                    return 204;
                }
                catch (Exception exn)
                {
                    _logger.LogError(exn, $"Failed to process video entry, operation may be retried. [messageId = {messageId}]");
                    // FIXME: Retryable error handling
                    return 204; // Message should not be retried...
                    // return 400; // Message should be retried...
                }
            }
            else if (entry.Operation == "thumbnail")
            {
                try
                {
                    await _videoResizer.Thumbnail(sourceUri, targetUri, new ResizeOptions(
                        imageType: entry.TargetType,
                        width: entry.TargetWidth,
                        height: entry.TargetHeight,
                        resizeMode: "inbox"
                    ), cancellationToken);
                    _logger.LogInformation("Successfully created thumbnail {0} => {1}.", entry.Source, entry.Target);
                    return 204;
                }
                catch (Exception exn)
                {
                    _logger.LogError(exn, $"Failed to process video(thumbnail) entry, operation may be retried. [messageId = {messageId}]");
                    // FIXME: Retryable error handling
                    return 204; // Message should not be retried...
                    // return 400; // Message should be retried...
                }
            }
            else
            {
                _logger.LogError("Unsupported video operation = {0}.", entry.Operation);
                return 204; // Message should not be retried...
            }
        }

        public ValueTask<int> ProcessAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(entry.EntryType))
            {
                _logger.LogError($"Failed to process entry: missing entry type. [messageId = {messageId}]");
                return new ValueTask<int>(204); // Message should not be retried...
            }
            if (StringComparer.InvariantCultureIgnoreCase.Equals(entry.EntryType, MediaQueueEntryTypes.Image))
            {
                return new ValueTask<int>(ProcessImageAsync(entry, messageId, cancellationToken));
            }
            if (StringComparer.InvariantCultureIgnoreCase.Equals(entry.EntryType, MediaQueueEntryTypes.Video))
            {
                return new ValueTask<int>(ProcessVideoAsync(entry, messageId, cancellationToken));
            }
            _logger.LogError($"Failed to process entry: unsupported entry type = {entry.EntryType}. [messageId = {messageId}]");
            return new ValueTask<int>(204); // Message should not be retried...
        }
    }
}