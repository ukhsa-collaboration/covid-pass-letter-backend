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

    public ValidationResult Validate(
        Barcode<InternationalSchema> barcode,
        Bundle source)
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

        if (!schema.Vaccinations.All(currentVaccination => MatchVaccination(currentVaccination, barcode, source)))
        {
            return this.options.IncludePiiInErrors
                ? ValidationResult.Failed(
                    $"Invalid certificate: expected vaccinations in '{source.ToJson(Version.R4)}' to match '{JsonSerializer.Serialize(schema.Vaccinations)}'")
                : ValidationResult.Failed("Invalid certificate:  Some vaccinations don't match");
        }

        return ValidationResult.Successful();
    }

    private static bool MatchVaccination(
        Vaccination vaccination,
        Barcode<InternationalSchema> barcode,
        Bundle source)
    {
        var entry = source.Entry;

        return entry.Any(currentEntry => MatchLocation(currentEntry, vaccination))
               && entry.Any(currentEntry => MatchEntryVaccination(currentEntry, barcode, vaccination))
               && vaccination.CertificateIssuer == BarcodeConstants.NhsDigital
               && vaccination.DiseaseTargeted == BarcodeConstants.DiseaseTargeted;
    }

    private static bool MatchLocation(
        Bundle.EntryComponent e,
        Vaccination vaccination)
    {
        return vaccination.Country == BarcodeConstants.GB
               || (e.Resource is Location l && l.Address?.Country == vaccination.Country);
    }

    private static bool MatchEntryVaccination(
        Bundle.EntryComponent e,
        Barcode<InternationalSchema> barcode,
        Vaccination vaccination)
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
