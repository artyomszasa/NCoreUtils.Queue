using Microsoft.Extensions.Logging;

namespace NCoreUtils.Queue;

internal static partial class LoggingExtensions
{
    public const int SourceImageResolvingFailed = 11700;

    public const int DestinationImageResolvingFailed = 11701;

    public const int FailedToProcessImageEntry = 11702;

    public const int FailedToProcessImageEntryMayBeRetried = 11703;

    public const int VideoSourceResolvingFailed = 11704;

    public const int VideoDestinationResolvingFailed = 11705;

    public const int VideoProcessed = 11706;

    public const int FailedToProcessVideoEntryMayBeRetried = 11707;

    public const int ThumbnailCreated = 11708;

    public const int FailedToProcessVideoThumbnailEntryMayBeRetried = 11709;

    public const int UnsupportedVideo = 11710;

    public const int MissingMediaEntryType = 11711;

    public const int UnsupportedMediaEntryType = 11712;

    public const int ImageProcessed = 11713;

    [LoggerMessage(
        EventId = SourceImageResolvingFailed,
        EventName = nameof(SourceImageResolvingFailed),
        Level = LogLevel.Error,
        Message = "Failed to resolve source: {Message}."
    )]
    public static partial void LogSourceImageResolvingFailed(this ILogger logger, string message);

    [LoggerMessage(
        EventId = DestinationImageResolvingFailed,
        EventName = nameof(DestinationImageResolvingFailed),
        Level = LogLevel.Error,
        Message = "Failed to resolve target: {Message}."
    )]
    public static partial void LogDestinationImageResolvingFailed(this ILogger logger, string message);

    [LoggerMessage(
        EventId = FailedToProcessImageEntry,
        EventName = nameof(FailedToProcessImageEntry),
        Level = LogLevel.Error,
        Message = "Failed to process image entry. [messageId = {MessageId}]."
    )]
    public static partial void LogFailedToProcessImageEntry(
        this ILogger logger,
        Exception exn,
        string messageId);

    [LoggerMessage(
        EventId = FailedToProcessImageEntryMayBeRetried,
        EventName = nameof(FailedToProcessImageEntryMayBeRetried),
        Level = LogLevel.Error,
        Message = "Failed to process image entry, operation may be retried. [messageId = {MessageId}]"
    )]
    public static partial void LogFailedToProcessImageEntryMayBeRetried(
        this ILogger logger,
        Exception exn,
        string messageId);

    [LoggerMessage(
        EventId = ImageProcessed,
        EventName = nameof(ImageProcessed),
        Level = LogLevel.Information,
        Message = "Successfully processed image {Source} => {Target}."
    )]
    public static partial void LogImageProcessed(this ILogger logger, IReadableResource source, IWritableResource target);

    [LoggerMessage(
        EventId = VideoSourceResolvingFailed,
        EventName = nameof(VideoSourceResolvingFailed),
        Level = LogLevel.Error,
        Message = "Failed to resolve source: {Message}."
    )]
    public static partial void LogVideoSourceResolvingFailed(this ILogger logger, string message);

    [LoggerMessage(
        EventId = VideoDestinationResolvingFailed,
        EventName = nameof(VideoDestinationResolvingFailed),
        Level = LogLevel.Error,
        Message = "Failed to resolve target: {Message}."
    )]
    public static partial void LogVideoDestinationResolvingFailed(this ILogger logger, string message);

    [LoggerMessage(
        EventId = VideoProcessed,
        EventName = nameof(VideoProcessed),
        Level = LogLevel.Information,
        Message = "Successfully processed video {Source} => {Target}."
    )]
    public static partial void LogVideoProcessed(this ILogger logger, IReadableResource source, IWritableResource target);

    [LoggerMessage(
        EventId = FailedToProcessVideoEntryMayBeRetried,
        EventName = nameof(FailedToProcessVideoEntryMayBeRetried),
        Level = LogLevel.Error,
        Message = "Failed to process video entry, operation may be retried. [messageId = {MessageId}]"
    )]
    public static partial void LogFailedToProcessVideoEntryMayBeRetried(
        this ILogger logger,
        Exception exn,
        string messageId);

    [LoggerMessage(
        EventId = ThumbnailCreated,
        EventName = nameof(ThumbnailCreated),
        Level = LogLevel.Information,
        Message = "Successfully created thumbnail {Source} => {Target}."
    )]
    public static partial void LogThumbnailCreated(this ILogger logger, string? source, string? target);

    [LoggerMessage(
        EventId = FailedToProcessVideoThumbnailEntryMayBeRetried,
        EventName = nameof(FailedToProcessVideoThumbnailEntryMayBeRetried),
        Level = LogLevel.Error,
        Message = "Failed to process video(thumbnail) entry, operation may be retried. [messageId = {MessageId}]"
    )]
    public static partial void LogFailedToProcessVideoThumbnailEntryMayBeRetried(
        this ILogger logger,
        Exception exn,
        string messageId);

    [LoggerMessage(
        EventId = UnsupportedVideo,
        EventName = nameof(UnsupportedVideo),
        Level = LogLevel.Error,
        Message = "Unsupported video operation = {Operation}."
    )]
    public static partial void LogUnsupportedVideo(this ILogger logger, string? operation);

    [LoggerMessage(
        EventId = MissingMediaEntryType,
        EventName = nameof(MissingMediaEntryType),
        Level = LogLevel.Error,
        Message = "Failed to process entry: missing entry type. [messageId = {MessageId}]."
    )]
    public static partial void LogMissingMediaEntryType(this ILogger logger, string messageId);

    [LoggerMessage(
            EventId = UnsupportedMediaEntryType,
            EventName = nameof(UnsupportedMediaEntryType),
            Level = LogLevel.Error,
            Message = "Failed to process entry: unsupported entry type = {EntryType}. [messageId = {MessageId}]."
        )]
    public static partial void LogUnsupportedMediaEntryType(
            this ILogger logger,
            string entryType,
            string messageId);
}