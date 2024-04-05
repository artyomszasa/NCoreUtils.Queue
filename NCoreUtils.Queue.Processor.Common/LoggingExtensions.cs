using Microsoft.Extensions.Logging;

namespace NCoreUtils.Queue;

internal static partial class LoggingExtensions
{
    public const int FailedImageResolveSource = 11700;

    public const int FailedImageResolveDestination = 11701;

    public const int FailedProcessImageEntry = 11702;

    public const int FailedProcessImageEntryOperationRetried = 11703;

    public const int FailedVideoResolveSource = 11704;

    public const int FailedVideoResolveDestination = 11705;

    public const int ProcessedVideoSuccessfully = 11706;

    public const int FailedProcessVideoEntryOperationRetried = 11707;

    public const int CreateThumbnailSuccessfully = 11708;

    public const int FailedProcessVideoThumbnailEntryOperationRetried = 11709;

    public const int UnsupportedVideoOperation = 11710;

    public const int FailedProcessMissingMediaEntryType = 11711;

    public const int FailedProcessUnsupportedMediaEntryType = 11712;

    [LoggerMessage(
        EventId = FailedImageResolveSource,
        EventName = nameof(FailedImageResolveSource),
        Level = LogLevel.Error,
        Message = "Failed to resolve source: {Message}."
    )]
    public static partial void LogFailedImageResolveSource(this ILogger logger, string message);

    [LoggerMessage(
        EventId = FailedImageResolveDestination,
        EventName = nameof(FailedImageResolveDestination),
        Level = LogLevel.Error,
        Message = "Failed to resolve target: {Message}."
    )]
    public static partial void LogFailedImageResolveDestination(this ILogger logger, string message);

    [LoggerMessage(
        EventId = FailedProcessImageEntry,
        EventName = nameof(FailedProcessImageEntry),
        Level = LogLevel.Error,
        Message = "Failed to process image entry. [messageId = {MessageId}]."
    )]
    public static partial void LogFailedProcessImageEntry(
        this ILogger logger,
        Exception exn,
        string messageId);

    [LoggerMessage(
        EventId = FailedProcessImageEntryOperationRetried,
        EventName = nameof(FailedProcessImageEntryOperationRetried),
        Level = LogLevel.Error,
        Message = "Failed to process image entry, operation may be retried. [messageId = {MessageId}]"
    )]
    public static partial void LogFailedProcessImageEntryOperationRetried(
        this ILogger logger,
        Exception exn,
        string messageId);

    [LoggerMessage(
        EventId = FailedVideoResolveSource,
        EventName = nameof(FailedVideoResolveSource),
        Level = LogLevel.Error,
        Message = "Failed to resolve source: {Message}."
    )]
    public static partial void LogFailedVideoResolveSource(this ILogger logger, string message);

    [LoggerMessage(
        EventId = FailedVideoResolveDestination,
        EventName = nameof(FailedVideoResolveDestination),
        Level = LogLevel.Error,
        Message = "Failed to resolve target: {Message}."
    )]
    public static partial void LogFailedVideoResolveDestination(this ILogger logger, string message);

    [LoggerMessage(
        EventId = ProcessedVideoSuccessfully,
        EventName = nameof(ProcessedVideoSuccessfully),
        Level = LogLevel.Information,
        Message = "Successfully processed video {Source} => {Target}."
    )]
    public static partial void LogProcessedVideoSuccessfully(this ILogger logger, string? source, string? target);

    [LoggerMessage(
        EventId = FailedProcessVideoEntryOperationRetried,
        EventName = nameof(FailedProcessVideoEntryOperationRetried),
        Level = LogLevel.Error,
        Message = "Failed to process video entry, operation may be retried. [messageId = {MessageId}]"
    )]
    public static partial void LogFailedProcessVideoEntryOperationRetried(
        this ILogger logger,
        Exception exn,
        string messageId);

    [LoggerMessage(
        EventId = CreateThumbnailSuccessfully,
        EventName = nameof(CreateThumbnailSuccessfully),
        Level = LogLevel.Information,
        Message = "Successfully created thumbnail {Source} => {Target}."
    )]
    public static partial void LogCreateThumbnailSuccessfully(this ILogger logger, string? source, string? target);

    [LoggerMessage(
        EventId = FailedProcessVideoThumbnailEntryOperationRetried,
        EventName = nameof(FailedProcessVideoThumbnailEntryOperationRetried),
        Level = LogLevel.Error,
        Message = "Failed to process video(thumbnail) entry, operation may be retried. [messageId = {MessageId}]"
    )]
    public static partial void LogFailedProcessVideoThumbnailEntryOperationRetried(
        this ILogger logger,
        Exception exn,
        string messageId);

    [LoggerMessage(
        EventId = UnsupportedVideoOperation,
        EventName = nameof(UnsupportedVideoOperation),
        Level = LogLevel.Error,
        Message = "Unsupported video operation = {Operation}."
    )]
    public static partial void LogUnsupportedVideoOperation(this ILogger logger, string? operation);

    [LoggerMessage(
        EventId = FailedProcessMissingMediaEntryType,
        EventName = nameof(FailedProcessMissingMediaEntryType),
        Level = LogLevel.Error,
        Message = "Failed to process entry: missing entry type. [messageId = {MessageId}]."
    )]
    public static partial void LogFailedProcessMissingMediaEntryType(this ILogger logger, string messageId);

    [LoggerMessage(
            EventId = FailedProcessUnsupportedMediaEntryType,
            EventName = nameof(FailedProcessUnsupportedMediaEntryType),
            Level = LogLevel.Error,
            Message = "Failed to process entry: unsupported entry type = {EntryType}. [messageId = {MessageId}]."
        )]
    public static partial void LogFailedProcessUnsupportedMediaEntryType(
            this ILogger logger,
            string entryType,
            string messageId);
}