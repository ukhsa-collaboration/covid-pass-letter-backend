namespace CovidLetter.Backend.Common.Infrastructure.Barcodes;

using System.Text.Json.Serialization;

public class Name
{
    [JsonPropertyName("gn")]
    public string GivenName { get; set; } = default!;

    [JsonPropertyName("fn")]
    public string FamilyName { get; set; } = default!;
}
