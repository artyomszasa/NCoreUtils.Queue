using System.Text.Json.Serialization;

namespace NCoreUtils.Queue;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MediaQueueEntry))]
public partial class MediaProcessingQueueSerializerContext : JsonSerializerContext { }