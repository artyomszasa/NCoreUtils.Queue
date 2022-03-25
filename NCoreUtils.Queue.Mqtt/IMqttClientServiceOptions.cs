using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Queue
{
    public interface IMqttClientServiceOptions
    {
        JsonSerializerContext JsonSerializerContext { get; }

        string Topic { get; }
    }
}