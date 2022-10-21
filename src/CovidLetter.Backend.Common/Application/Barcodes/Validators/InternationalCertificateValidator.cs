namespace CovidLetter.Backend.Common.Application.Barcodes.Validators;

using System.Text.Json;
using CovidLetter.Backend.Common.Infrastructure.Barcodes;
using CovidLetter.Backend.Common.Options;
using Hl7.Fhir.Model;
using Hl7.Fhir.Model.R4;
using Hl7.Fhir.Serialization;

public class InternationalCertificateValidator
    : IBarcodeValidator<InternationalSchema, Bundle>
{
    private readonly BarcodeOptions options;

    public InternationalCertificateValidator(BarcodeOptions options)
    {
        this.options = options;
    }

    public ValidationResult Validate(Barcode<InternationalSchema> barcode, Bundle source)
    {
        var schema = barcode.Certificate.HCert.Schema;

        if (schema.Version != this.options.InternationalEuDccVersion)
        {
            return ValidationResult.Failed("Invalid certificate: version is not valid: " + schema.Version);
        }

        if (schema.Vaccinations is null or { Count: 0 })
        {
            return ValidationResult.Failed("Invalid certificate: No vaccinations");
        }

        if (!schema.Vaccinations.All(MatchVaccination))
        {
            return this.options.IncludePiiInErrors
                ? ValidationResult.Failed(
                    $"Invalid certificate: expected vaccinations in '{source.ToJson(Version.R4)}' to match '{JsonSerializer.Serialize(schema.Vaccinations)}'")
                : ValidationResult.Failed("Invalid certificate:  Some vaccinations don't match");
        }

        return ValidationResult.Successful();

        bool MatchVaccination(Vaccination vaccination)
        {
            var entry = source.Entry;

            return entry.Any(MatchLocation)
                   && entry.Any(MatchVaccination)
                   && vaccination.CertificateIssuer == BarcodeConstants.NhsDigital
                   && vaccination.DiseaseTargeted == BarcodeConstants.DiseaseTargeted;

            bool MatchLocation(Bundle.EntryComponent e)
            {
                return vaccination.Country == BarcodeConstants.GB
                       || (e.Resource is Location l && l.Address?.Country == vaccination.Country);
            }

            bool MatchVaccination(Bundle.EntryComponent e)
            {
                if (e.Resource is not Immunization i)
                {
                    return false;
                }

                if (i.Id != barcode.Id)
                {
                    return false;
                }

                if (i.Occurrence is not FhirDateTime)
                {
                    return false;
                }

                if (i.ProtocolApplied[0].DoseNumber is not PositiveInt dn)
                {
                    return false;
                }

                if (dn.Value != vaccination.DoseNumber)
                {
                    return false;
                }

                if (i.ProtocolApplied[0].SeriesDoses is not PositiveInt sd)
                {
                    return false;
                }

                if (sd.Value != vaccination.TotalNumberOfDose)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
