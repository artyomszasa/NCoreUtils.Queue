namespace NCoreUtils.Queue;

internal static partial class LoggingExtensions
{
    public const int EnqueuedSuccessfully = 6000;

    [LoggerMessage(
        EventId = EnqueuedSuccessfully,
        EventName = nameof(EnqueuedSuccessfully),
        Level = LogLevel.Information,
        Message = "Successfully enqueued entry {Entry} => {MessageId}."
    )]
    public static partial void LogEnqueuedSuccessfully(this ILogger logger, MediaQueueEntry entry, string messageId);
}