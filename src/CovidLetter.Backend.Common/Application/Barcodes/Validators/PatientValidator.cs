namespace CovidLetter.Backend.Common.Application.Barcodes.Validators;

using System.Text.Json;
using CovidLetter.Backend.Common.Infrastructure.Barcodes;
using CovidLetter.Backend.Common.Options;
using Hl7.Fhir.Model.R4;

public class PatientValidator<TCertificate, TSource>
    : IBarcodeValidator<TCertificate, TSource>
    where TCertificate : Schema
{
    private readonly BarcodeOptions options;
    private readonly Func<TSource, Patient> patientSelector;
    private readonly bool validateName;

    public PatientValidator(BarcodeOptions options, Func<TSource, Patient> patientSelector, bool validateName)
    {
        this.options = options;
        this.patientSelector = patientSelector;
        this.validateName = validateName;
    }

    public ValidationResult Validate(Barcode<TCertificate> barcode, TSource source)
    {
        var schema = barcode.Certificate.HCert.Schema;
        var patient = this.patientSelector(source);

        if (schema.DateOfBirth?.Date != patient.BirthDateElement.ToDateTimeOffset()?.Date)
        {
            return this.options.IncludePiiInErrors
                ? ValidationResult.Failed(
                    $"Invalid certificate: expected DoB '{JsonSerializer.Serialize(patient.BirthDate)}' to match '{JsonSerializer.Serialize(schema.DateOfBirth)}'")
                : ValidationResult.Failed("Invalid certificate:  DoB doesnt match");
        }

        if (this.validateName && !patient.Name.Any(n => FamilyNameMatches(n, schema) && GivenNameMatches(n, schema)))
        {
            return this.options.IncludePiiInErrors
                ? ValidationResult.Failed(
                    $"Invalid certificate: expected patient name '{JsonSerializer.Serialize(patient.Name)}' to match '{JsonSerializer.Serialize(schema.Name)}'")
                : ValidationResult.Failed("Invalid certificate: patient name doesnt match");
        }

        return ValidationResult.Successful();
    }

    private static bool GivenNameMatches(HumanName n, Schema schema)
    {
        var normalized = string.Join(" ", n.Given);
        return string.Equals(normalized, schema.Name.GivenName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool FamilyNameMatches(HumanName n, Schema schema)
    {
        return string.Equals(n.Family, schema.Name.FamilyName, StringComparison.OrdinalIgnoreCase);
    }
}
