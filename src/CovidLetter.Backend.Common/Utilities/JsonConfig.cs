namespace CovidLetter.Backend.Common.Utilities;

using System.Text.Json;
using System.Text.Json.Serialization;
using CovidLetter.Backend.Common.Application.Serialization;

public static class JsonConfig
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new NullableConverterFactory() },
    };
}
