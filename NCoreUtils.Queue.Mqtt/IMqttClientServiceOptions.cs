using System.Text.Json;

namespace NCoreUtils.Queue
{
    public interface IMqttClientServiceOptions
    {
        JsonSerializerOptions JsonSerializerOptions { get; }

        string Topic { get; }
    }
}