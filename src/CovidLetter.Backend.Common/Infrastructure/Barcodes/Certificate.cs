namespace CovidLetter.Backend.Common.Infrastructure.Barcodes;

using System.Text.Json.Serialization;

public class Certificate
{
    [JsonPropertyName("df")]
    public DateTime? DateFrom { get; set; }

    [JsonPropertyName("du")]
    public DateTime? DateUntil { get; set; }

    [JsonPropertyName("co")]
    public string Country { get; set; } = default!;

    [JsonPropertyName("pm")]
    public byte CertificateType { get; set; } = default;

    [JsonPropertyName("is")]
    public string CertificateIssuer { get; set; } = default!;

    [JsonPropertyName("po")]
    public List<string> PolicyApplied { get; set; } = default!;
}
