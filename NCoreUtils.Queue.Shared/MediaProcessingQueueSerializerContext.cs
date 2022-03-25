using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Queue.Internal;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MediaQueueEntry))]
public partial class MediaProcessingQueueSerializerContext : JsonSerializerContext
{
    internal JsonSerializerOptions? GetGeneratedSerializerOptions() => GeneratedSerializerOptions;
}