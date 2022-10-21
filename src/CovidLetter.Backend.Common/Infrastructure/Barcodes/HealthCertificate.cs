namespace CovidLetter.Backend.Common.Infrastructure.Barcodes;

using System.Text.Json.Serialization;

public class HealthCertificate<TSchema>
{
    [JsonPropertyName("1")]
    public string Iss { get; set; } = default!;

    [JsonPropertyName("6")]
    public int Iat { get; set; }

    [JsonPropertyName("4")]
    public int Exp { get; set; }

    [JsonPropertyName("-260")]
    public HCert<TSchema> HCert { get; set; } = default!;
}
