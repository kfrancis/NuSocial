using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NostrLib.Models;

namespace NostrLib.Converters
{
    public class NostrEventTagJsonConverter : JsonConverter<NostrEventTag>
    {
        public override NostrEventTag? Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            var result = new NostrEventTag();
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Nostr Event Tags are an array");
            }

            reader.Read();
            for (var i = 0; reader.TokenType != JsonTokenType.EndArray; i++)
            {
                if (i == 0)
                {
                    result.TagIdentifier = StringEscaperJsonConverter.JavaScriptStringEncode(reader.GetString() ?? string.Empty, false);
                }
                else
                {
                    result.Data.Add(StringEscaperJsonConverter.JavaScriptStringEncode(reader.GetString() ?? string.Empty, false));
                }

                reader.Read();
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, NostrEventTag value, JsonSerializerOptions options)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            writer.WriteStringValue(value.TagIdentifier);
            value.Data?.ForEach(writer.WriteStringValue);
            writer.WriteEndArray();
        }
    }
}
