using System.Text.Json.Serialization;

namespace NCoreUtils.Queue;

[JsonSerializable(typeof(PubSubRequest))]
[JsonSerializable(typeof(PubSubMessage))]
public partial class MediaSerializerContext : JsonSerializerContext
{

}