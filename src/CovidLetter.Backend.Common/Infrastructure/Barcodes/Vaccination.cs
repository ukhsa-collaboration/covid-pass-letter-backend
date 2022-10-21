namespace CovidLetter.Backend.Common.Infrastructure.Barcodes;

using System.Text.Json.Serialization;

public class Vaccination
{
    [JsonPropertyName("tg")]
    public string DiseaseTargeted { get; set; } = default!;

    [JsonPropertyName("dn")]

    public int DoseNumber { get; set; }

    [JsonPropertyName("sd")]
    public int TotalNumberOfDose { get; set; }

    [JsonPropertyName("dt")]
    public DateTime? DateOfVaccination { get; set; }

    [JsonPropertyName("co")]
    public string Country { get; set; } = default!;

    [JsonPropertyName("is")]
    public string CertificateIssuer { get; set; } = default!;

    [JsonPropertyName("ci")]
    public string CertificateIdentifier { get; set; } = default!;
}
