using Microsoft.Extensions.Logging;

namespace NCoreUtils.Queue;

internal static partial class LoggingExtensions
{
    public const int SubscriberClientMessageProcessStarted = 11900;

    public const int SubscriberClientMessageProcessStoppedSuccessfully = 11905;

    public const int SubscriberClientSuccess = 11906;

    public const int SubscriberClientUnprocessableReason = 11907;

    public const int SubscriberClientUnprocessableException = 11908;

    public const int SubscriberClientFailedReason = 11909;

    public const int SubscriberClientFailedException = 11910;

    public const int SubscriberClientNoAckId = 11911;

    public const int SubscriberClientAcknowlegeFailed = 11912;

    public const int SubscriberClientAcknowlegeSuccess = 11913;

    public const int SubscriberClientWorkerFailed = 11914;

    public const int SubscriberClientReceivedMessages = 11915;

    public const int SubscriberClientPullFailed = 11916;

    [LoggerMessage(
        EventId = SubscriberClientMessageProcessStarted,
        EventName = nameof(SubscriberClientMessageProcessStarted),
        Level = LogLevel.Debug,
        Message = "Start processing messages."
    )]
    public static partial void LogSubscriberClientMessageProcessStarted(this ILogger logger);

    [LoggerMessage(
        EventId = SubscriberClientMessageProcessStoppedSuccessfully,
        EventName = nameof(SubscriberClientMessageProcessStoppedSuccessfully),
        Level = LogLevel.Debug,
        Message = "Processing messages stopped succefully."
    )]
    public static partial void LogSubscriberClientMessageProcessStoppedSuccessfully(this ILogger logger);

    [LoggerMessage(
        EventId = SubscriberClientSuccess,
        EventName = nameof(SubscriberClientSuccess),
        Level = LogLevel.Debug,
        Message = "Message processed successfully [MessageId = {MessageId}]."
    )]
    public static partial void LogSubscriberClientSuccess(this ILogger logger, string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientUnprocessableReason,
        EventName = nameof(SubscriberClientUnprocessableReason),
        Level = LogLevel.Error,
        Message = "Message unprocessable: {Reason} [MessageId = {MessageId}]."
    )]
    public static partial void LogSubscriberClientUnprocessableReason(this ILogger logger, string reason, string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientUnprocessableException,
        EventName = nameof(SubscriberClientUnprocessableException),
        Level = LogLevel.Error,
        Message = "Message unprocessable [MessageId = {MessageId}]."
    )]
    public static partial void LogSubscriberClientUnprocessableException(this ILogger logger, Exception exn, string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientFailedReason,
        EventName = nameof(SubscriberClientFailedReason),
        Level = LogLevel.Error,
        Message = "Message processing failed (and may be retried): {Reason} [MessageId = {MessageId}]."
    )]
    public static partial void LogSubscriberClientFailedReason(this ILogger logger, string reason, string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientFailedException,
        EventName = nameof(SubscriberClientFailedException),
        Level = LogLevel.Error,
        Message = "Message processing failed (and may be retried) [MessageId = {MessageId}]."
    )]
    public static partial void LogSubscriberClientFailedException(this ILogger logger, Exception exn, string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientNoAckId,
        EventName = nameof(SubscriberClientNoAckId),
        Level = LogLevel.Warning,
        Message = "Message should be acknowleged but no ackId found [MessageId = {MessageId}]."
    )]
    public static partial void LogSubscriberClientNoAckId(this ILogger logger, string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientAcknowlegeFailed,
        EventName = nameof(SubscriberClientAcknowlegeFailed),
        Level = LogLevel.Warning,
        Message = "Message should be acknowleged but sending acknowlege failed [MessageId = {MessageId}]."
    )]
    public static partial void LogSubscriberClientAcknowlegeFailed(this ILogger logger, Exception exn, string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientAcknowlegeSuccess,
        EventName = nameof(SubscriberClientAcknowlegeSuccess),
        Level = LogLevel.Debug,
        Message = "Acknowlege sent for [AckId = {AckId}, MessageId = {MessageId}]."
    )]
    public static partial void LogSubscriberClientAcknowlegeSuccess(this ILogger logger, string ackId, string? messageId);

    [LoggerMessage(
        EventId = SubscriberClientWorkerFailed,
        EventName = nameof(SubscriberClientWorkerFailed),
        Level = LogLevel.Error,
        Message = "Message processing failed due to exception."
    )]
    public static partial void LogSubscriberClientWorkerFailed(this ILogger logger, Exception exn);

    [LoggerMessage(
        EventId = SubscriberClientReceivedMessages,
        EventName = nameof(SubscriberClientReceivedMessages),
        Level = LogLevel.Debug,
        Message = "Received {Count} messages.")]
    public static partial void LogSubscriberClientReceivedMessages(this ILogger logger, int count);

    [LoggerMessage(
        EventId = SubscriberClientPullFailed,
        EventName = nameof(SubscriberClientPullFailed),
        Level = LogLevel.Error,
        Message = "Pub/Sub pull has failed.")]
    public static partial void LogSubscriberClientPullFailed(this ILogger logger, Exception exception);


}