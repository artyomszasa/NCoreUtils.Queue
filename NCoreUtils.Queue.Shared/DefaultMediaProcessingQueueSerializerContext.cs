using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.Queue.Internal;

public class DefaultMediaProcessingQueueSerializerContext : JsonSerializerContext
{
    private MediaProcessingQueueSerializerContext Context { get; }

    public DefaultMediaProcessingQueueSerializerContext()
        : this(null)
    { }

    public DefaultMediaProcessingQueueSerializerContext(JsonSerializerOptions? options)
        : base(null)
    {
        Context = options is null ? MediaProcessingQueueSerializerContext.Default : new MediaProcessingQueueSerializerContext(options);
    }

    protected override JsonSerializerOptions? GeneratedSerializerOptions => Context.GetGeneratedSerializerOptions();

    public override JsonTypeInfo? GetTypeInfo(Type type)
        => Context.GetTypeInfo(type);
}