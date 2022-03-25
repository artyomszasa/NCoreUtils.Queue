using System.Text.Json;
using NCoreUtils.AspNetCore.Proto;

namespace NCoreUtils.Queue.Internal
{
    public static class MediaProcessingQueueProtoConfiguration
    {
        public static void Configure(ServiceDescriptorBuilder builder, string? path)
            => builder
                .SetPath(path ?? string.Empty)
                .SetNamingPolicy(NamingConvention.SnakeCase)
                .SetDefaultInputType(InputType.Json<DefaultMediaProcessingQueueSerializerContext>());

        public static void Configure(ServiceDescriptorBuilder builder)
            => Configure(builder, default);

    }
}