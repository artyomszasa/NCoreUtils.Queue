using Microsoft.AspNetCore.Mvc;
using MQTTnet.Client;

namespace NCoreUtils.Queue;

internal static partial class LoggingExtensions
{
    public const int EnqueuedEntrySuccessfully = 11600;

    public const int MQTTClientConnectSuccessfully = 11601;

    public const int MQTTClientDisconnected = 11602;

    public const int MQTTServiceStartedSuccessfully = 11603;

    public const int MQTTServiceAlreadyRunning = 11604;

    public const int MQTTServiceNotRunning = 11605;

    public const int MQTTServiceStoppedSuccessfully = 11606;

    [LoggerMessage(
        EventId = EnqueuedEntrySuccessfully,
        EventName = nameof(EnqueuedEntrySuccessfully),
        Level = LogLevel.Information,
        Message = "Successfully enqueued entry {Entry} => {MessageId}."
    )]
    public static partial void LogEnqueuedEntrySuccessfully(this ILogger logger, MediaQueueEntry entry, int? messageId);

    [LoggerMessage(
        EventId = MQTTClientConnectSuccessfully,
        EventName = nameof(MQTTClientConnectSuccessfully),
        Level = LogLevel.Debug,
        Message =  "MQTT client created and connected successfully (result code = {ResultCode}, response = {ResponseInformation})."
    )]
    public static partial void LogMQTTClientConnectSuccessfully(this ILogger logger ,
        MqttClientConnectResultCode resultCode,
        string responseInformation);

    [LoggerMessage(
        EventId = MQTTClientDisconnected,
        EventName = nameof(MQTTClientDisconnected),
        Level = LogLevel.Warning,
        Message = "MQTT client has disconnected, reason: {Reason}, trying to reconnect."
    )]
    public static partial void LogMQTTClientDisconnected(this ILogger logger, Exception exn, MqttClientDisconnectReason reason);

    [LoggerMessage(
        EventId = MQTTServiceStartedSuccessfully,
        EventName = nameof(MQTTServiceStartedSuccessfully),
        Level = LogLevel.Debug,
        Message = "MQTT service started successfully."
    )]
    public static partial void LogMQTTServiceStartedSuccessfully(this ILogger logger);

    [LoggerMessage(
        EventId = MQTTServiceAlreadyRunning,
        EventName = nameof(MQTTServiceAlreadyRunning),
        Level = LogLevel.Warning,
        Message = "MQTT service is already running."
    )]
    public static partial void LogMQTTServiceAlreadyRunning(this ILogger logger);

    [LoggerMessage(
        EventId = MQTTServiceNotRunning,
        EventName = nameof(MQTTServiceNotRunning),
        Level = LogLevel.Warning,
        Message = "MQTT service is not running."
    )]
    public static partial void LogMQTTServiceNotRunning(this ILogger logger);

    [LoggerMessage(
        EventId = MQTTServiceStoppedSuccessfully,
        EventName = nameof(MQTTServiceStoppedSuccessfully),
        Level = LogLevel.Debug,
        Message = "MQTT stopped successfully."
    )]
    public static partial void LogMQTTServiceStoppedSuccessfully(this ILogger logger);
}

