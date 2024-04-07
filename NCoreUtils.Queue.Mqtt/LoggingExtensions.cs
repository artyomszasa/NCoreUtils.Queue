using Microsoft.AspNetCore.Mvc;
using MQTTnet.Client;

namespace NCoreUtils.Queue;

internal static partial class LoggingExtensions
{
    public const int EntryEnqueued = 11600;

    public const int MqttClientConnected = 11601;

    public const int MqttClientDisconnected = 11602;

    public const int MqttServiceStarted = 11603;

    public const int MqttServiceAlreadyRunning = 11604;

    public const int MqttServiceNotRunning = 11605;

    public const int MqttServiceStopped = 11606;

    [LoggerMessage(
        EventId = EntryEnqueued,
        EventName = nameof(EntryEnqueued),
        Level = LogLevel.Information,
        Message = "Successfully enqueued entry {Entry} => {MessageId}."
    )]
    public static partial void LogEntryEnqueued(this ILogger logger, MediaQueueEntry entry, int? messageId);

    [LoggerMessage(
        EventId = MqttClientConnected,
        EventName = nameof(MqttClientConnected),
        Level = LogLevel.Debug,
        Message =  "MQTT client created and connected successfully (result code = {ResultCode}, response = {ResponseInformation})."
    )]
    public static partial void LogMqttClientConnected(this ILogger logger ,
        MqttClientConnectResultCode resultCode,
        string responseInformation);

    [LoggerMessage(
        EventId = MqttClientDisconnected,
        EventName = nameof(MqttClientDisconnected),
        Level = LogLevel.Warning,
        Message = "MQTT client has disconnected, reason: {Reason}, trying to reconnect."
    )]
    public static partial void LogMqttClientDisconnected(this ILogger logger, Exception exn, MqttClientDisconnectReason reason);

    [LoggerMessage(
        EventId = MqttServiceStarted,
        EventName = nameof(MqttServiceStarted),
        Level = LogLevel.Debug,
        Message = "MQTT service started successfully."
    )]
    public static partial void LogMqttServiceStarted(this ILogger logger);

    [LoggerMessage(
        EventId = MqttServiceAlreadyRunning,
        EventName = nameof(MqttServiceAlreadyRunning),
        Level = LogLevel.Warning,
        Message = "MQTT service is already running."
    )]
    public static partial void LogMqttServiceAlreadyRunning(this ILogger logger);

    [LoggerMessage(
        EventId = MqttServiceNotRunning,
        EventName = nameof(MqttServiceNotRunning),
        Level = LogLevel.Warning,
        Message = "MQTT service is not running."
    )]
    public static partial void LogMqttServiceNotRunning(this ILogger logger);

    [LoggerMessage(
        EventId = MqttServiceStopped,
        EventName = nameof(MqttServiceStopped),
        Level = LogLevel.Debug,
        Message = "MQTT stopped successfully."
    )]
    public static partial void LogMqttServiceStopped(this ILogger logger);
}

