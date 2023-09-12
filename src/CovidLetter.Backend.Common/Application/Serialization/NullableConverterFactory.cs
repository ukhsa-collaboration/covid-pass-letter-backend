namespace CovidLetter.Backend.Common.Application.Serialization;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// courtesy https://stackoverflow.com/a/65025191
public class NullableConverterFactory : JsonConverterFactory
{
    private static readonly byte[] Empty = Array.Empty<byte>();

    public override bool CanConvert(Type typeToConvert) => Nullable.GetUnderlyingType(typeToConvert) != null;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(NullableConverter<>).MakeGenericType(Nullable.GetUnderlyingType(typeToConvert)!),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: new object[] { options },
            culture: null)!;

    private class NullableConverter<T> : JsonConverter<T?>
        where T : struct
    {
        public NullableConverter(JsonSerializerOptions options)
        {
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                return JsonSerializer.Deserialize<T>(ref reader, options);
            }

            if (reader.ValueTextEquals(Empty))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value!.Value, options);
    }
}
