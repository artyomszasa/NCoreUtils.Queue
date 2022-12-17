namespace NCoreUtils.Queue;

public record MqttClientServiceOptions(string Topic) : IMqttClientServiceOptions;