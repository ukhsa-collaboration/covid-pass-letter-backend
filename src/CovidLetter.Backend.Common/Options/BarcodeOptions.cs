namespace CovidLetter.Backend.Common.Options;

public class BarcodeOptions
{
    public byte DomesticCertificateType { get; set; }

    public string DomesticPolicyApplied { get; set; } = null!;

    public string DevolvedAdministrationPublicKeysEndpointUrl { get; set; } = null!;

    public string InternationalEuDccVersion { get; set; } = string.Empty;

    public string DomesticEuDccVersion { get; set; } = string.Empty;

    public string DevolvedAdministrationPublicKeysBaseUrl { get; set; } = string.Empty;

    public bool IncludePiiInErrors { get; set; }

    public string UnattendedCertificateApiBaseUrl { get; set; } = null!;

    public string UnattendedCertificateVaccinationEndpointPath { get; set; } = null!;

    public string UnattendedCertificateApiKeyHeaderName { get; set; } = string.Empty;

    public string UnattendedCertificateApiKeyHeaderValueGb { get; set; } = string.Empty;

    public string UnattendedCertificateApiKeyHeaderValueIm { get; set; } = string.Empty;

    public string UnattendedCertificateApiKeyHeaderValueWales { get; set; } = string.Empty;

    public string UnattendedCertificateDomesticEndpointPath { get; set; } = string.Empty;

    public string UnattendedCertificateRecoveryEndpointPath { get; set; } = string.Empty;

    public int UnattendedCertificateApiMaxParallelization { get; set; } = 1;
}
