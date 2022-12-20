﻿using System;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace NostrLib.Converters
{
    public class UnixTimestampSecondsJsonConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException("datetime was not in number format");
            }

            return DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue(value.Value.ToUnixTimeSeconds());
            }
        }
    }
}
