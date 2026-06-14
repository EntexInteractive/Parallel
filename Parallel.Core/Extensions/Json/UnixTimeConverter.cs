// Copyright 2026 Entex Interactive

using Newtonsoft.Json;
using Parallel.Core.Utils;

namespace Parallel.Core.Extensions.Json
{
    public class UnixTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsValueType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            UnixTime unix;
            if (reader.TokenType == JsonToken.Integer)
            {
                unix = UnixTime.FromMilliseconds(Convert.ToInt64(reader.Value));
            }
            else if (reader.TokenType == JsonToken.String)
            {
                unix = UnixTime.Parse((string?)reader.Value);
            }
            else
            {
                throw new JsonSerializationException($"Unexpected token parsing date. Expected Integer or String, got {reader.TokenType}");
            }

            return unix;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            long seconds;
            if (value is UnixTime unixTime)
            {
                seconds = unixTime.TotalMilliseconds;
            }
            else
            {
                throw new JsonSerializationException("Expected date object value.");
            }

            writer.WriteValue(seconds);
        }
    }
}