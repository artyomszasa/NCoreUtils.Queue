using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace NCoreUtils.Queue
{
    public class MqttClientServiceOptions : IMqttClientServiceOptions
    {
        public JsonSerializerContext JsonSerializerContext { get; }

        public string Topic { get; set; } = default!;

        public MqttClientServiceOptions(JsonSerializerContext jsonSerializerContext)
            => JsonSerializerContext = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));
    }
}