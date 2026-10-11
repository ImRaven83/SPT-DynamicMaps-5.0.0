using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace DynamicMaps.Utils
{
    /// <summary>
    /// The game's Newtonsoft.Json only exists inside il2cpp, so map/config json goes through System.Text.Json
    /// with converters for the il2cpp Unity value types (they can't be (de)serialized by reflection)
    /// </summary>
    public static class JsonUtils
    {
        public static readonly JsonSerializerOptions Options = CreateOptions(false);
        public static readonly JsonSerializerOptions IndentedOptions = CreateOptions(true);

        private static JsonSerializerOptions CreateOptions(bool indented)
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                WriteIndented = indented,
            };

            options.Converters.Add(new Vector2Converter());
            options.Converters.Add(new Vector3Converter());
            options.Converters.Add(new ColorConverter());

            return options;
        }

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }

        public static string Serialize<T>(T value, bool indented = true)
        {
            return JsonSerializer.Serialize(value, indented ? IndentedOptions : Options);
        }

        /// <summary>
        /// Reads a flat object of named floats, e.g. {"x": 1, "y": 2}, calling setter for each property
        /// </summary>
        private static void ReadFloatObject(ref Utf8JsonReader reader, Action<string, float> setter)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected start of object, got {reader.TokenType}");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Expected property name, got {reader.TokenType}");
                }

                var name = reader.GetString();
                reader.Read();
                setter(name.ToLowerInvariant(), reader.GetSingle());
            }

            throw new JsonException("Unexpected end of json while reading object");
        }

        private class Vector2Converter : JsonConverter<Vector2>
        {
            public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                float x = 0, y = 0;
                ReadFloatObject(ref reader, (name, value) =>
                {
                    if (name == "x") x = value;
                    else if (name == "y") y = value;
                });
                return new Vector2(x, y);
            }

            public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("x", value.x);
                writer.WriteNumber("y", value.y);
                writer.WriteEndObject();
            }
        }

        private class Vector3Converter : JsonConverter<Vector3>
        {
            public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                float x = 0, y = 0, z = 0;
                ReadFloatObject(ref reader, (name, value) =>
                {
                    if (name == "x") x = value;
                    else if (name == "y") y = value;
                    else if (name == "z") z = value;
                });
                return new Vector3(x, y, z);
            }

            public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("x", value.x);
                writer.WriteNumber("y", value.y);
                writer.WriteNumber("z", value.z);
                writer.WriteEndObject();
            }
        }

        private class ColorConverter : JsonConverter<Color>
        {
            public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                float r = 0, g = 0, b = 0, a = 1;
                ReadFloatObject(ref reader, (name, value) =>
                {
                    if (name == "r") r = value;
                    else if (name == "g") g = value;
                    else if (name == "b") b = value;
                    else if (name == "a") a = value;
                });
                return new Color(r, g, b, a);
            }

            public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("r", value.r);
                writer.WriteNumber("g", value.g);
                writer.WriteNumber("b", value.b);
                writer.WriteNumber("a", value.a);
                writer.WriteEndObject();
            }
        }
    }
}
