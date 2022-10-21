namespace CovidLetter.Backend.Common.Infrastructure.Barcodes;

using System.Text.Json.Serialization;

public class Schema
{
    [JsonPropertyName("dob")]
    public DateTime? DateOfBirth { get; set; }

    [JsonPropertyName("nam")]
    public Name Name { get; set; } = default!;

    [JsonPropertyName("ver")]
    public string Version { get; set; } = default!;
}

public class InternationalSchema : Schema
{
    [JsonPropertyName("v")]
    public List<Vaccination> Vaccinations { get; set; } = default!;
}

public class DomesticSchema : Schema
{
    [JsonPropertyName("d")]
    public List<Certificate> Certificates { get; set; } = default!;
}

public class RecoverySchema : Schema
{
    [JsonPropertyName("r")]
    public List<Recovery> Certificates { get; set; } = default!;
}
