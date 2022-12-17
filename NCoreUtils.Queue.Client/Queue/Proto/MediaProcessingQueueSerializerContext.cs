using System.Text.Json.Serialization;

namespace NCoreUtils.Queue.Proto;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JsonRootMediaProcessingQueueInfo))]
public partial class MediaProcessingQueueSerializerContext : JsonSerializerContext { }