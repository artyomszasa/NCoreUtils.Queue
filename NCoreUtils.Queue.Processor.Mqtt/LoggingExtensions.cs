using Microsoft.Extensions.Logging;
using MQTTnet.Client;

namespace NCoreUtils.Queue;

internal static partial class LoggingExtensions
{
    public const int MqttApplicationMessageReceivedFailure = 11800;

    public const int MqttClientConnectedSuccessfully = 11801;

    public const int MqttClientSubscribedSuccessfully = 11802;

    public const int MqttClientDisconnected = 11803;

    public const int MqttServiceStarted = 11804;

    public const int MqttServiceRunning = 11805;

    public const int MqttServiceNotRunning = 11806;

    public const int MqttServiceStopped = 11807;

    [LoggerMessage(
            EventId = MqttApplicationMessageReceivedFailure,
            EventName = nameof(MqttApplicationMessageReceivedFailure),
            Level = LogLevel.Error,
            Message = "Failed to process entry."
        )]
    public static partial void LogMqttApplicationMessageReceivedFailure(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = MqttClientConnectedSuccessfully,
        EventName = nameof(MqttClientConnectedSuccessfully),
        Level = LogLevel.Debug,
        Message = "MQTT client created and connected successfully (result code = {ResultCode}, response = {ResponseInformation})."
    )]
    public static partial void LogMqttClientConnectedSuccessfully(
        this ILogger logger,
        MqttClientConnectResultCode resultCode,
        string responseInformation);

    [LoggerMessage(
        EventId = MqttClientSubscribedSuccessfully,
        EventName = nameof(MqttClientSubscribedSuccessfully),
        Level = LogLevel.Debug,
        Message = "MQTT client successfully subscribed to topic {Topic}."
    )]
    public static partial void LogMqttClientSubscribedSuccessfully(this ILogger logger, string topic);

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
        EventId = MqttServiceRunning,
        EventName = nameof(MqttServiceRunning),
        Level = LogLevel.Warning,
        Message = "MQTT service is already running."
    )]
    public static partial void LogMqttServiceRunning(this ILogger logger);

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
