using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Buffers;

namespace KoenZomers.Ring.Api.Entities
{
    internal class FlexibleStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Use JsonDocument.ParseValue to reliably consume the current token (whatever its type)
            try
            {
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    var elem = doc.RootElement;
                    switch (elem.ValueKind)
                    {
                        case JsonValueKind.String:
                            return elem.GetString();
                        case JsonValueKind.Number:
                            return elem.GetRawText();
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            return elem.GetBoolean().ToString();
                        case JsonValueKind.Null:
                        case JsonValueKind.Undefined:
                            return null;
                        case JsonValueKind.Object:
                        case JsonValueKind.Array:
                        default:
                            return elem.GetRawText();
                    }
                }
            }
            catch
            {
                // Fallback: return null if anything unexpected occurs
                return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}
