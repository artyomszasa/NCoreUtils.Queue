using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Queue.Internal
{
    public class MediaQueueEntryConverter : JsonConverter<MediaQueueEntry>
    {
        public static MediaQueueEntryConverter Instance { get; } = new MediaQueueEntryConverter();

        private MediaQueueEntryConverter() { }

        public override MediaQueueEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new InvalidOperationException($"Expected {JsonTokenType.StartObject}, got {reader.TokenType}.");
            }
            reader.Read();
            string? entryType = default;
            string? source = default;
            string? target = default;
            string? operation = default;
            int? targetWidth = default;
            int? targetHeight = default;
            int? weightX = default;
            int? weightY = default;
            string? targetType = default;
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException($"Expected {JsonTokenType.PropertyName}, got {reader.TokenType}.");
                }
                if (reader.ValueTextEquals("entryType"))
                {
                    reader.Read();
                    entryType = reader.GetString();
                    reader.Read();
                }
                else if (reader.ValueTextEquals("source"))
                {
                    reader.Read();
                    source = reader.GetStringOrNull();
                    reader.Read();
                }
                else if (reader.ValueTextEquals("target"))
                {
                    reader.Read();
                    target = reader.GetStringOrNull();
                    reader.Read();
                }
                else if (reader.ValueTextEquals("operation"))
                {
                    reader.Read();
                    operation = reader.GetStringOrNull();
                    reader.Read();
                }
                else if (reader.ValueTextEquals("targetType"))
                {
                    reader.Read();
                    targetType = reader.GetStringOrNull();
                    reader.Read();
                }
                else if (reader.ValueTextEquals("targetWidth"))
                {
                    reader.Read();
                    targetWidth = reader.GetInt32OrDefault();
                    reader.Read();
                }
                else if (reader.ValueTextEquals("targetHeight"))
                {
                    reader.Read();
                    targetHeight = reader.GetInt32OrDefault();
                    reader.Read();
                }
                else if (reader.ValueTextEquals("weightX"))
                {
                    reader.Read();
                    weightX = reader.GetInt32OrDefault();
                    reader.Read();
                }
                else if (reader.ValueTextEquals("weightY"))
                {
                    reader.Read();
                    weightY = reader.GetInt32OrDefault();
                    reader.Read();
                }
                else
                {
                    reader.Read();
                    reader.Skip();
                    reader.Read();
                }
            }
            return new MediaQueueEntry(entryType!, source, target, operation, targetWidth, targetHeight, weightX, weightY, targetType);
        }

        public override void Write(Utf8JsonWriter writer, MediaQueueEntry value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("entryType", value.EntryType);
            writer.WriteStringWhenNotNull("source", value.Source);
            writer.WriteStringWhenNotNull("target", value.Target);
            writer.WriteStringWhenNotNull("operation", value.Operation);
            writer.WriteNumberWhenNotNull("targetWidth", value.TargetWidth);
            writer.WriteNumberWhenNotNull("targetHeight", value.TargetHeight);
            writer.WriteNumberWhenNotNull("weightX", value.WeightX);
            writer.WriteNumberWhenNotNull("weightY", value.WeightY);
            writer.WriteStringWhenNotNull("targetType", value.TargetType);
            writer.WriteEndObject();
        }
    }
}