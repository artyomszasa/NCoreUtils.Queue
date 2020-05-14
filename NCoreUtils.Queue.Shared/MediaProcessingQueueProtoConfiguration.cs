using System.Text.Json;
using NCoreUtils.AspNetCore.Proto;

namespace NCoreUtils.Queue.Internal
{
    public static class MediaProcessingQueueProtoConfiguration
    {
        public static void Configure(ServiceDescriptorBuilder builder, string? path)
            => builder
                .SetPath(path)
                .SetNamingPolicy(NamingPolicy.SnakeCase)
                .SetDefaultInputType(InputType.Json(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { MediaQueueEntryConverter.Instance }
                }));

        public static void Configure(ServiceDescriptorBuilder builder)
            => Configure(builder, default);

    }
}