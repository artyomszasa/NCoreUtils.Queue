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
        protected abstract class Either<TLeft, TRight>
        {
            public sealed class Left : Either<TLeft, TRight>
            {
                public TLeft Value { get; }

                public Left(TLeft value) => Value = value;
            }

            public sealed class Right : Either<TLeft, TRight>
            {
                public TRight Value { get; }

                public Right(TRight value) => Value = value;
            }
        }

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

        protected virtual Either<IImageSource, string> ResolveSource(string? source, string messageId)
        {
            if (source is null)
            {
                return new Either<IImageSource, string>.Right($"Failed to process entry: missing or invalid source uri = {source}. [messageId = {messageId}]");
            }
            if (source.StartsWith("/"))
            {
                source = "file://" + source;
            }
            if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
            {
                return new Either<IImageSource, string>.Right($"Failed to process entry: missing or invalid source uri = {source}. [messageId = {messageId}]");
            }
            if (sourceUri.Scheme == "gs")
            {
                return new Either<IImageSource, string>.Left(new GoogleCloudStorageSource(sourceUri));
            }
            if (sourceUri.Scheme == "file")
            {
                return new Either<IImageSource, string>.Left(new FileSystemSource(sourceUri.AbsolutePath));
            }
            return new Either<IImageSource, string>.Right($"Failed to process entry: unsupported source uri = {source}. [messageId = {messageId}]");
        }

        protected virtual Either<IImageDestination, string> ResolveDestination(string? destination, string messageId)
        {
            if (destination is null)
            {
                return new Either<IImageDestination, string>.Right($"Failed to process entry: missing or invalid destination uri = {destination}. [messageId = {messageId}]");
            }
            if (destination.StartsWith("/"))
            {
                destination = "file://" + destination;
            }
            if (!Uri.TryCreate(destination, UriKind.Absolute, out var destinationUri))
            {
                return new Either<IImageDestination, string>.Right($"Failed to process entry: missing or invalid destination uri = {destination}. [messageId = {messageId}]");
            }
            if (destinationUri.Scheme == "gs")
            {
                return new Either<IImageDestination, string>.Left(new GoogleCloudStorageDestination(destinationUri, isPublic: true, cacheControl: "private, max-age=31536000"));
            }
            if (destinationUri.Scheme == "file")
            {
                return new Either<IImageDestination, string>.Left(new FileSystemDestination(destinationUri.AbsolutePath));
            }
            return new Either<IImageDestination, string>.Right($"Failed to process entry: unsupported destination uri = {destination}. [messageId = {messageId}]");
        }

        public async Task<int> ProcessImageAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
        {
            IImageSource source;
            IImageDestination destination;
            switch (ResolveSource(entry.Source, messageId))
            {
                case Either<IImageSource, string>.Left l:
                    source = l.Value;
                    break;
                case Either<IImageSource, string>.Right r:
                    Logger.LogError(r.Value);
                    return 204; // Message should not be retried...
                default:
                    throw new InvalidOperationException("Should never happen");
            }
            switch (ResolveDestination(entry.Target, messageId))
            {
                case Either<IImageDestination, string>.Left l:
                    destination = l.Value;
                    break;
                case Either<IImageDestination, string>.Right r:
                    Logger.LogError(r.Value);
                    return 204; // Message should not be retried...
                default:
                    throw new InvalidOperationException("Should never happen");
            }
            try
            {
                await _imageResizer.ResizeAsync(
                    source,
                    destination,
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