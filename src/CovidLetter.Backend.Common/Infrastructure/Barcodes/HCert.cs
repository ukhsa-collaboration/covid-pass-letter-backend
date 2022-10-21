namespace CovidLetter.Backend.Common.Infrastructure.Barcodes;

using System.Text.Json.Serialization;

public class HCert<TSchema>
{
    [JsonPropertyName("1")]
    public TSchema Schema { get; set; } = default!;
}
