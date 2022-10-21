namespace CovidLetter.Backend.Common.Application.Barcodes.Validators;

using CovidLetter.Backend.Common.Infrastructure.Barcodes;
using CovidLetter.Backend.Common.Options;
using Hl7.Fhir.Model.R4;

public class DomesticCertificateValidator
    : IBarcodeValidator<DomesticSchema, Patient>
{
    private readonly BarcodeOptions options;

    public DomesticCertificateValidator(BarcodeOptions options)
    {
        this.options = options;
    }

    public ValidationResult Validate(Barcode<DomesticSchema> barcode, Patient source)
    {
        var iat = DateTime.UnixEpoch.AddSeconds(barcode.Certificate.Iat).Date;
        var exp = DateTime.UnixEpoch.AddSeconds(barcode.Certificate.Exp).Date;

        var schema = barcode.Certificate.HCert.Schema;

        if (schema.Version != this.options.DomesticEuDccVersion)
        {
            return ValidationResult.Failed("Invalid certificate: version is not valid: " + schema.Version);
        }

        if (schema.Certificates is null or { Count: 0 } || !schema.Certificates.All(MatchCertificate))
        {
            return ValidationResult.Failed("Invalid certificate: Some certificates are not valid.");
        }

        return ValidationResult.Successful();

        bool MatchCertificate(Certificate arg)
        {
            return
                arg.CertificateType == this.options.DomesticCertificateType
                && arg.CertificateIssuer == BarcodeConstants.NhsDigital
                && MatchPoliciesApplied(arg.PolicyApplied)
                && arg.DateFrom?.Date == iat.Date
                && arg.DateUntil?.Date == exp.Date;

            bool MatchPoliciesApplied(List<string> appliedPolicies)
            {
                if (string.IsNullOrWhiteSpace(this.options.DomesticPolicyApplied))
                {
                    return appliedPolicies.Count == 0;
                }

                var optionsPolicies = this.options.DomesticPolicyApplied.Split(',');

                if (appliedPolicies.Count != optionsPolicies.Length)
                {
                    return false;
                }

                return appliedPolicies.Except(optionsPolicies!).Any() == false;
            }
        }
    }
}
