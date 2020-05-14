using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Queue.Internal
{
    public class MediaQueueEntriesConverter : JsonConverter<IEnumerable<MediaQueueEntry>>
    {
        public override IEnumerable<MediaQueueEntry> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new InvalidOperationException($"Expected {JsonTokenType.StartArray}, got {reader.TokenType}.");
            }
            reader.Read();
            var entries = new List<MediaQueueEntry>();
            var entryConverter = MediaQueueEntryConverter.Instance;
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                entries.Add(entryConverter.Read(ref reader, typeof(MediaQueueEntry), options));
                reader.Read();
            }
            return entries;
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<MediaQueueEntry> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            var entryConverter = MediaQueueEntryConverter.Instance;
            foreach (var entry in value)
            {
                entryConverter.Write(writer, entry, options);
            }
            writer.WriteEndArray();
        }
    }
}