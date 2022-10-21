// ReSharper disable InconsistentNaming
namespace CovidLetter.Backend.Common.Application.Certificates;

public class TestResultNhs
{
    public string ProcessingCode { get; set; } = null!;

    public string TestKit { get; set; } = null!;

    public Tuple<string, string> DiseaseTargeted { get; set; } = null!;

    public string Authority { get; set; } = null!;

    public string? CountryOfAuthority { get; set; } = null!;

    public string? TestLocation { get; set; } = null!;

    public string? RAT { get; set; } = null!;

    public string? TestType { get; set; } = null!;

    public bool IsNAAT { get; set; }

    public string? CountryCode { get; set; } = null!;

    public DateTime DateTimeOfTest { get; set; }

    public string Result { get; set; } = null!;

    public string? ValidityType { get; set; } = null!;
}
