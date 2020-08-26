using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
// using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images;
using NCoreUtils.Queue.Internal;

namespace NCoreUtils.Queue
{
    public class MediaEntryProcessor
    {
        public ILogger Logger { get; }

        private readonly IImageResizer _imageResizer;

        private readonly IVideoResizer _videoResizer;

        public MediaEntryProcessor(ILogger<MediaEntryProcessor> logger, IImageResizer imageResizer, IVideoResizer videoResizer)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageResizer = imageResizer ?? throw new ArgumentNullException(nameof(imageResizer));
            _videoResizer = videoResizer ?? throw new ArgumentNullException(nameof(videoResizer));
        }

        /*

        */

        public async Task<int> ProcessImageAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(entry.Source, UriKind.Absolute, out var sourceUri))
            {
                Logger.LogError($"Failed to process entry: missing or invalid source uri = {entry.Source}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (!Uri.TryCreate(entry.Target, UriKind.Absolute, out var targetUri))
            {
                Logger.LogError($"Failed to process entry: missing or invalid target uri = {entry.Target}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (sourceUri.Scheme != "gs")
            {
                Logger.LogError($"Failed to process entry: unsupported source uri = {entry.Source}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (targetUri.Scheme != "gs")
            {
                Logger.LogError($"Failed to process entry: unsupported target uri = {entry.Target}. [messageId = {messageId}]");
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
                Logger.LogError(exn, $"Failed to process image entry. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            catch (UnsupportedImageTypeException exn)
            {
                Logger.LogError(exn, $"Failed to process image entry. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            catch (UnsupportedResizeModeException exn)
            {
                Logger.LogError(exn, $"Failed to process image entry. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            catch (Exception exn)
            {
                Logger.LogError(exn, $"Failed to process image entry, operation may be retried. [messageId = {messageId}]");
                return 400; // Message should be retried...
            }
            return 200;
        }

        public async Task<int> ProcessVideoAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(entry.Source, UriKind.Absolute, out var sourceUri))
            {
                Logger.LogError($"Failed to process entry: missing or invalid source uri = {entry.Source}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (!Uri.TryCreate(entry.Target, UriKind.Absolute, out var targetUri))
            {
                Logger.LogError($"Failed to process entry: missing or invalid target uri = {entry.Target}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (sourceUri.Scheme != "gs")
            {
                Logger.LogError($"Failed to process entry: unsupported source uri = {entry.Source}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (targetUri.Scheme != "gs")
            {
                Logger.LogError($"Failed to process entry: unsupported target uri = {entry.Target}. [messageId = {messageId}]");
                return 204; // Message should not be retried...
            }
            if (string.IsNullOrEmpty(entry.Operation) || entry.Operation == "resize" || entry.Operation.StartsWith("watermark:"))
            {
                string? watermark = entry.Operation is null
                    ? default
                    : entry.Operation.StartsWith("watermark:")
                        ? entry.Operation.Substring("watermark:".Length)
                        : default;
                try
                {
                    await _videoResizer.ResizeAsync(sourceUri, targetUri, new Videos.VideoOptions(
                        entry.TargetType ?? "mp4",
                        entry.TargetWidth,
                        entry.TargetHeight,
                        75,
                        watermark), cancellationToken);
                    Logger.LogInformation("Successfully processed video {0} => {1}.", entry.Source, entry.Target);
                    return 204;
                }
                catch (Exception exn)
                {
                    Logger.LogError(exn, $"Failed to process video entry, operation may be retried. [messageId = {messageId}]");
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
                    Logger.LogInformation("Successfully created thumbnail {0} => {1}.", entry.Source, entry.Target);
                    return 204;
                }
                catch (Exception exn)
                {
                    Logger.LogError(exn, $"Failed to process video(thumbnail) entry, operation may be retried. [messageId = {messageId}]");
                    // FIXME: Retryable error handling
                    return 204; // Message should not be retried...
                    // return 400; // Message should be retried...
                }
            }
            else
            {
                Logger.LogError("Unsupported video operation = {0}.", entry.Operation);
                return 204; // Message should not be retried...
            }
        }

        public ValueTask<int> ProcessAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(entry.EntryType))
            {
                Logger.LogError($"Failed to process entry: missing entry type. [messageId = {messageId}]");
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
            Logger.LogError($"Failed to process entry: unsupported entry type = {entry.EntryType}. [messageId = {messageId}]");
            return new ValueTask<int>(204); // Message should not be retried...
        }
    }
}