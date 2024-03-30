using System.Text.Json.Serialization;

namespace NCoreUtils.Queue;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MediaQueueEntry))]
internal partial class MediaProcessingQueueSerializerContext : JsonSerializerContext { }