using Microsoft.Extensions.Logging;

namespace NCoreUtils.Queue;

internal static partial class LoggingExtensions
{
    public const int SubscriberClientMessageProcessStarted = 11900;

    public const int SubscriberClientProcessMessage = 11901;

    public const int SubscriberClientDeserializeFailed = 11902;

    public const int SubscriberClientReplyMessage = 11903;

    public const int SubscriberClientReplyMessageFailed = 11904;

    public const int SubscriberClientMessageProcessStoppedSuccessfully = 11905;

    [LoggerMessage(
        EventId = SubscriberClientMessageProcessStarted,
        EventName = nameof(SubscriberClientMessageProcessStarted),
        Level = LogLevel.Debug,
        Message = "Start processing messages."
    )]
    public static partial void LogSubscriberClientMessageProcessStarted(this ILogger logger);

    [LoggerMessage(
        EventId = SubscriberClientProcessMessage,
        EventName = nameof(SubscriberClientProcessMessage),
        Level = LogLevel.Debug,
        Message = "Processing message {MessageId}."
    )]
    public static partial void LogSubscriberClientProcessMessage(this ILogger logger, string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientDeserializeFailed,
        EventName = nameof(SubscriberClientDeserializeFailed),
        Level = LogLevel.Error,
        Message ="Failed to deserialize pub/sub message {MessageId}."
    )]
    public static partial void LogSubscriberClientDeserializeFailed(
        this ILogger logger,
        Exception exception,
        string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientReplyMessage,
        EventName = nameof(SubscriberClientReplyMessage),
        Level = LogLevel.Debug,
        Message ="Processed message {MessageId} => {Ack}."
    )]
    public static partial void LogSubscriberClientReplyMessage(
        this ILogger logger,
        string? messageId,
        object ack);

    [LoggerMessage(
        EventId = SubscriberClientReplyMessageFailed,
        EventName = nameof(SubscriberClientReplyMessageFailed),
        Level = LogLevel.Error,
        Message = "Failed to process pub/sub message {MessageId}."
    )]
    public static partial void LogSubscriberClientReplyMessageFailed(
        this ILogger logger,
        Exception exception,
        string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientMessageProcessStoppedSuccessfully,
        EventName = nameof(SubscriberClientMessageProcessStoppedSuccessfully),
        Level = LogLevel.Debug,
        Message = "Processing messages stopped succefully."
    )]
    public static partial void LogSubscriberClientMessageProcessStoppedSuccessfully(this ILogger logger);


}

/* sablon
    [LoggerMessage(
        EventId = ,
        EventName = nameof(),
        Level = LogLevel.,
        Message =
    )]
    public static partial void Log(this ILogger logger);

*/