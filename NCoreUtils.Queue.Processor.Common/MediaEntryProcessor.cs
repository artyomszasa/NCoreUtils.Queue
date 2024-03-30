using Microsoft.Extensions.Logging;
using NCoreUtils.Images;
using NCoreUtils.Resources;
using NCoreUtils.Videos;

namespace NCoreUtils.Queue;

public class MediaEntryProcessor(
    ILogger<MediaEntryProcessor> logger,
    IImageResizer imageResizer,
    IVideoResizer videoResizer,
    IResourceFactory resourceFactory,
    GoogleCloudStorageUtils googleCloudStorageUtils,
    IFallbackAccessTokenProvider? fallbackAccessTokenProvider = default)
{
    protected abstract class Either<TLeft, TRight>
    {
        public sealed class Left(TLeft value) : Either<TLeft, TRight>
        {
            public TLeft Value { get; } = value;
        }

        public sealed class Right(TRight value) : Either<TLeft, TRight>
        {
            public TRight Value { get; } = value;
        }
    }

    public ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    private IImageResizer ImageResizer { get; } = imageResizer ?? throw new ArgumentNullException(nameof(imageResizer));

    private IVideoResizer VideoResizer { get; } = videoResizer ?? throw new ArgumentNullException(nameof(videoResizer));

    private IResourceFactory ResourceFactory { get; } = resourceFactory ?? throw new ArgumentNullException(nameof(resourceFactory));

    private GoogleCloudStorageUtils GoogleCloudStorageUtils { get; } = googleCloudStorageUtils ?? throw new ArgumentNullException(nameof(googleCloudStorageUtils));

    private IFallbackAccessTokenProvider? FallbackAccessTokenProvider { get; } = fallbackAccessTokenProvider;

    protected virtual Either<IReadableResource, string> ResolveSource(string? source, string messageId)
    {
        if (source is null)
        {
            return new Either<IReadableResource, string>.Right($"Failed to process entry: missing or invalid source uri = {source}. [messageId = {messageId}]");
        }
        if (source.StartsWith('/'))
        {
            source = "file://" + source;
        }
        if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
        {
            return new Either<IReadableResource, string>.Right($"Failed to process entry: missing or invalid source uri = {source}. [messageId = {messageId}]");
        }
        if (ResourceFactory.TryCreateReadable(sourceUri, out var resource))
        {
            return new Either<IReadableResource, string>.Left(resource);
        }
        // if (sourceUri.Scheme == "gs")
        // {
        //     return new Either<IReadableResource, string>.Left(new GoogleCloudStorageResource(
        //         utils: GoogleCloudStorageUtils,
        //         accessTokenProvider: accessTokenProvider,
        //         bucketName: sourceUri.Host,
        //         objectName: sourceUri.AbsolutePath.Trim('/'),
        //         logger: Logger
        //     ));
        // }
        // if (sourceUri.Scheme == "file")
        // {
        //     return new Either<IReadableResource, string>.Left(new FileSystemResource(sourceUri.AbsolutePath, default));
        // }
        return new Either<IReadableResource, string>.Right($"Failed to process entry: unsupported source uri = {source}. [messageId = {messageId}]");
    }

    protected virtual Either<IWritableResource, string> ResolveDestination(string? destination, string messageId)
    {
        if (destination is null)
        {
            return new Either<IWritableResource, string>.Right($"Failed to process entry: missing or invalid destination uri = {destination}. [messageId = {messageId}]");
        }
        if (destination.StartsWith('/'))
        {
            destination = "file://" + destination;
        }
        if (!Uri.TryCreate(destination, UriKind.Absolute, out var destinationUri))
        {
            return new Either<IWritableResource, string>.Right($"Failed to process entry: missing or invalid destination uri = {destination}. [messageId = {messageId}]");
        }
        if (ResourceFactory.TryCreateWritable(destinationUri, out var resource))
        {
            return new Either<IWritableResource, string>.Left(resource);
        }
        // if (destinationUri.Scheme == "gs")
        // {
        //     return new Either<IWritableResource, string>.Left(new GoogleCloudStorageResource(
        //         utils: GoogleCloudStorageUtils,
        //         accessTokenProvider: AccessTokenProvider,
        //         bucketName: destinationUri.Host,
        //         objectName: destinationUri.AbsolutePath.Trim('/'),
        //         isPublic: true,
        //         cacheControl: "private, max-age=31536000",
        //         logger: Logger
        //     ));
        // }
        // if (destinationUri.Scheme == "file")
        // {
        //     return new Either<IWritableResource, string>.Left(new FileSystemResource(destinationUri.AbsolutePath, default));
        // }
        return new Either<IWritableResource, string>.Right($"Failed to process entry: unsupported destination uri = {destination}. [messageId = {messageId}]");
    }

    public async Task<int> ProcessImageAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
    {
        IReadableResource source;
        IWritableResource destination;
        switch (ResolveSource(entry.Source, messageId))
        {
            case Either<IReadableResource, string>.Left l:
                source = l.Value;
                break;
            case Either<IReadableResource, string>.Right r:
                Logger.LogError("Failed to resolve source: {Message}.", r.Value);
                return 204; // Message should not be retried...
            default:
                throw new InvalidOperationException("Should never happen");
        }
        switch (ResolveDestination(entry.Target, messageId))
        {
            case Either<IWritableResource, string>.Left l:
                destination = l.Value;
                break;
            case Either<IWritableResource, string>.Right r:
                Logger.LogError("Failed to resolve target: {Message}.", r.Value);
                return 204; // Message should not be retried...
            default:
                throw new InvalidOperationException("Should never happen");
        }
        try
        {
            await ImageResizer.ResizeAsync(
                source,
                destination,
                new Images.ResizeOptions(
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
        catch (ImageException exn)
            when (exn.Message.Contains("Forbidden")
                && destination is GoogleCloudStorageResource gsDestination
                && FallbackAccessTokenProvider is IFallbackAccessTokenProvider fallbackAccessTokenProvider)
        {
            // retry with fallback credentials...
            var newDestination = new GoogleCloudStorageResource(
                utils: GoogleCloudStorageUtils,
                accessTokenProvider: fallbackAccessTokenProvider,
                bucketName: gsDestination.BucketName,
                objectName: gsDestination.ObjectName,
                contentType: gsDestination.ContentType,
                cacheControl: gsDestination.CacheControl,
                isPublic: gsDestination.IsPublic,
                logger: Logger
            );
            try
            {
                await ImageResizer.ResizeAsync(
                    source,
                    newDestination,
                    new Images.ResizeOptions(
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
            catch (InvalidImageException exn1)
            {
                Logger.LogError(exn1, "Failed to process image entry. [messageId = {MessageId}].", messageId);
                return 204; // Message should not be retried...
            }
            catch (UnsupportedImageTypeException exn1)
            {
                Logger.LogError(exn1, "Failed to process image entry. [messageId = {MessageId}].", messageId);
                return 204; // Message should not be retried...
            }
            catch (Images.UnsupportedResizeModeException exn1)
            {
                Logger.LogError(exn1, "Failed to process image entry. [messageId = {MessageId}].", messageId);
                return 204; // Message should not be retried...
            }
            catch (ImageException exn1)
            {
                Logger.LogError(exn1, "Failed to process image entry. [messageId = {MessageId}]", messageId);
                return 204; // Message should be retried...
            }
            catch (Exception exn1)
            {
                Logger.LogError(exn1, "Failed to process image entry, operation may be retried. [messageId = {MessageId}]", messageId);
                return 400; // Message should be retried...
            }
            return 200;
        }
        catch (InvalidImageException exn)
        {
            Logger.LogError(exn, "Failed to process image entry. [messageId = {MessageId}].", messageId);
            return 204; // Message should not be retried...
        }
        catch (UnsupportedImageTypeException exn)
        {
            Logger.LogError(exn, "Failed to process image entry. [messageId = {MessageId}].", messageId);
            return 204; // Message should not be retried...
        }
        catch (Images.UnsupportedResizeModeException exn)
        {
            Logger.LogError(exn, "Failed to process image entry. [messageId = {MessageId}].", messageId);
            return 204; // Message should not be retried...
        }
        catch (Exception exn)
        {
            Logger.LogError(exn, "Failed to process image entry, operation may be retried. [messageId = {MessageId}]", messageId);
            return 400; // Message should be retried...
        }
        return 200;
    }

    public async Task<int> ProcessVideoAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
    {
        IReadableResource source;
        IWritableResource destination;
        switch (ResolveSource(entry.Source, messageId))
        {
            case Either<IReadableResource, string>.Left l:
                source = l.Value;
                break;
            case Either<IReadableResource, string>.Right r:
                Logger.LogError("Failed to resolve source: {Message}.", r.Value);
                return 204; // Message should not be retried...
            default:
                throw new InvalidOperationException("Should never happen");
        }
        switch (ResolveDestination(entry.Target, messageId))
        {
            case Either<IWritableResource, string>.Left l:
                destination = l.Value;
                break;
            case Either<IWritableResource, string>.Right r:
                Logger.LogError("Failed to resolve target: {Message}.", r.Value);
                return 204; // Message should not be retried...
            default:
                throw new InvalidOperationException("Should never happen");
        }
        if (string.IsNullOrEmpty(entry.Operation)
            || entry.Operation == "inbox"
            || entry.Operation == "exact"
            || entry.Operation == "none")
        {
            try
            {
                await VideoResizer.ResizeAsync(
                    source: source,
                    destination: destination,
                    options: new Videos.ResizeOptions(
                        audioType: default,
                        videoType: VideoSettings.TryParse(entry.TargetType, default, out var settings)
                            ? settings
                            : new X264Settings(bitRate: default, pixelFormat: default, preset: "veryslow"),
                        width: entry.TargetWidth,
                        height: entry.TargetHeight,
                        resizeMode: entry.Operation ?? "none",
                        quality: default,
                        optimize: true,
                        weightX: entry.WeightX,
                        weightY: entry.WeightY
                    ),
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
                Logger.LogInformation("Successfully processed video {Source} => {Target}.", entry.Source, entry.Target);
                return 204;
            }
            catch (Exception exn)
            {
                Logger.LogError(exn, "Failed to process video entry, operation may be retried. [messageId = {MessageId}].", messageId);
                // FIXME: Retryable error handling
                return 204; // Message should not be retried...
                // return 400; // Message should be retried...
            }
        }
        else if (entry.Operation == "thumbnail")
        {
            try
            {
                await VideoResizer.CreateThumbnailAsync(
                    source: source,
                    destination: destination,
                    options: new Videos.ResizeOptions(
                        width: entry.TargetWidth,
                        height: entry.TargetHeight,
                        resizeMode: "inbox"
                    ),
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
                Logger.LogInformation("Successfully created thumbnail {Source} => {Target}.", entry.Source, entry.Target);
                return 204;
            }
            catch (Exception exn)
            {
                Logger.LogError(exn, "Failed to process video(thumbnail) entry, operation may be retried. [messageId = {MessageId}].", messageId);
                // FIXME: Retryable error handling
                return 204; // Message should not be retried...
                // return 400; // Message should be retried...
            }
        }
        else
        {
            Logger.LogError("Unsupported video operation = {Operation}.", entry.Operation);
            return 204; // Message should not be retried...
        }
    }

    public ValueTask<int> ProcessAsync(MediaQueueEntry entry, string messageId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entry.EntryType))
        {
            Logger.LogError("Failed to process entry: missing entry type. [messageId = {MessageId}].", messageId);
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
        Logger.LogError("Failed to process entry: unsupported entry type = {EntryType}. [messageId = {MessageId}].", entry.EntryType, messageId);
        return new ValueTask<int>(204); // Message should not be retried...
    }
}