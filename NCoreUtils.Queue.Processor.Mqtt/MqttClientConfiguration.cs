namespace NCoreUtils.Queue;

public record MqttClientConfiguration(
    string? Host,
    int? Port,
    bool? CleanSession,
    string? ClientId
);