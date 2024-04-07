using Microsoft.Extensions.Logging;
using MQTTnet.Client;

namespace NCoreUtils.Queue;

internal static partial class LoggingExtensions
{
    public const int MqttFailedToReceiveMessage = 11800;

    public const int MqttClientConnected = 11801;

    public const int MqttClientSubscribed = 11802;

    public const int MqttClientDisconnected = 11803;

    public const int MqttServiceStarted = 11804;

    public const int MqttServiceAlreadyRunning = 11805;

    public const int MqttServiceNotRunning = 11806;

    public const int MqttServiceStopped = 11807;

    [LoggerMessage(
            EventId = MqttFailedToReceiveMessage,
            EventName = nameof(MqttFailedToReceiveMessage),
            Level = LogLevel.Error,
            Message = "Failed to process entry."
        )]
    public static partial void LogMqttFailedToReceiveMessage(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = MqttClientConnected,
        EventName = nameof(MqttClientConnected),
        Level = LogLevel.Debug,
        Message = "MQTT client created and connected successfully (result code = {ResultCode}, response = {ResponseInformation})."
    )]
    public static partial void LogMqttClientConnected(
        this ILogger logger,
        MqttClientConnectResultCode resultCode,
        string responseInformation);

    [LoggerMessage(
        EventId = MqttClientSubscribed,
        EventName = nameof(MqttClientSubscribed),
        Level = LogLevel.Debug,
        Message = "MQTT client successfully subscribed to topic {Topic}."
    )]
    public static partial void LogMqttClientSubscribed(this ILogger logger, string topic);

    [LoggerMessage(
        EventId = MqttClientDisconnected,
        EventName = nameof(MqttClientDisconnected),
        Level = LogLevel.Warning,
        Message = "MQTT client has disconnected, reason: {Reason}, trying to reconnect."
    )]
    public static partial void LogMqttClientDisconnected(
        this ILogger logger,
        Exception exception,
        MqttClientDisconnectReason reason);

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
