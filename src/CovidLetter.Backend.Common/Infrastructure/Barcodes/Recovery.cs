namespace CovidLetter.Backend.Common.Infrastructure.Barcodes;

using System.Text.Json.Serialization;

public class Recovery
{
    [JsonPropertyName("tg")]
    public string DiseaseTargeted { get; set; } = default!;

    [JsonPropertyName("fr")]
    public DateTime? DateOfPositiveTestResult { get; set; }

    [JsonPropertyName("co")]
    public string Country { get; set; } = default!;

    [JsonPropertyName("is")]
    public string CertificateIssuer { get; set; } = default!;

    [JsonPropertyName("df")]
    public DateTime? ValidFrom { get; set; }

    [JsonPropertyName("du")]
    public DateTime? ValidUntil { get; set; }

    [JsonPropertyName("ci")]
    public string CertificateIdentifier { get; set; } = default!;
}
