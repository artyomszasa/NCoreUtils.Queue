using System.Text.Json.Serialization;

namespace NCoreUtils.Queue;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PubSubRequest))]
[JsonSerializable(typeof(PubSubMessage))]
public partial class MediaSerializerContext : JsonSerializerContext
{

}