using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.Queue;

public interface IMqttClientService : IHostedService
{
    Task<int?> PublishAsync<T>(T payload, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken);
}