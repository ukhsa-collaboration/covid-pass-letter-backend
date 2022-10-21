namespace CovidLetter.Backend.Common.Application.Certificates.Domestic;

public class DomesticCertificate
{
    public string Name { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    public DateTime? ValidityStartDate { get; set; }

    public DateTime ValidityEndDate { get; set; }

    public DateTime EligibilityEndDate { get; set; }

    public CertificateType CertificateType { get; set; }

    public CertificateScenario CertificateScenario { get; set; }

    public List<string> QrCodeTokens { get; set; } = null!;

    public string UniqueCertificateIdentifier { get; set; } = null!;

    public int? PolicyMask { get; set; }

    public string[] Policy { get; set; } = null!;

    public string PKICountry { get; set; } = null!;

    public string Issuer { get; set; } = null!;

    public string Country { get; set; } = null!;
}
