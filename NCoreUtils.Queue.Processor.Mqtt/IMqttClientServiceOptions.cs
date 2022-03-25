using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Queue
{
    public interface IMqttClientServiceOptions
    {
        string Topic { get; }
    }
}