namespace CovidLetter.Backend.Common.Application.Barcodes.Validators;

using CovidLetter.Backend.Common.Infrastructure.Barcodes;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Hl7.Fhir.Model.R4;

public static class BarcodeValidatorFactory
{
    public static BarcodeValidator<InternationalSchema, Bundle> MakeInternational(
        IReadOnlyDictionary<string, string> publicKeys, BarcodeOptions options, IClock clock)
    {
        return new BarcodeValidator<InternationalSchema, Bundle>(new IBarcodeValidator<InternationalSchema, Bundle>[]
        {
            new SignatureValidator<InternationalSchema, Bundle>(publicKeys),
            new HealthCertificateValidator<InternationalSchema, Bundle>(options, clock),
            new InternationalCertificateValidator(options),
            new PatientValidator<InternationalSchema, Bundle>(
                options,
                b => b.Entry.Select(e => e.Resource).OfType<Patient>().First(),
                false),
        });
    }

    public static BarcodeValidator<DomesticSchema, Patient> MakeDomestic(
        IReadOnlyDictionary<string, string> publicKeys, BarcodeOptions options, IClock clock)
    {
        return new BarcodeValidator<DomesticSchema, Patient>(new IBarcodeValidator<DomesticSchema, Patient>[]
        {
            new SignatureValidator<DomesticSchema, Patient>(publicKeys),
            new HealthCertificateValidator<DomesticSchema, Patient>(options, clock),
            new DomesticCertificateValidator(options),
            new PatientValidator<DomesticSchema, Patient>(options, p => p, true),
        });
    }

    public static BarcodeValidator<InternationalSchema, Patient> MakeInternationalCertificate(
        IReadOnlyDictionary<string, string> publicKeys, BarcodeOptions options)
    {
        return new BarcodeValidator<InternationalSchema, Patient>(new IBarcodeValidator<InternationalSchema, Patient>[]
        {
            new SignatureValidator<InternationalSchema, Patient>(publicKeys),
            new PatientValidator<InternationalSchema, Patient>(options, p => p, false),
            new ChecksExpIsInFutureCertificateValidator<InternationalSchema, Patient>(),
        });
    }

    public static BarcodeValidator<DomesticSchema, Patient> MakeDomesticCertificate(
        IReadOnlyDictionary<string, string> publicKeys, BarcodeOptions options)
    {
        return new BarcodeValidator<DomesticSchema, Patient>(new IBarcodeValidator<DomesticSchema, Patient>[]
        {
            new SignatureValidator<DomesticSchema, Patient>(publicKeys),
            new PatientValidator<DomesticSchema, Patient>(options, p => p, false),
            new ChecksExpIsInFutureCertificateValidator<DomesticSchema, Patient>(),
        });
    }

    public static BarcodeValidator<RecoverySchema, Patient> MakeRecoveryCertificate(
        IReadOnlyDictionary<string, string> publicKeys, BarcodeOptions options)
    {
        return new BarcodeValidator<RecoverySchema, Patient>(new IBarcodeValidator<RecoverySchema, Patient>[]
        {
            new SignatureValidator<RecoverySchema, Patient>(publicKeys),
            new PatientValidator<RecoverySchema, Patient>(options, p => p, false),
            new ChecksExpIsInFutureCertificateValidator<RecoverySchema, Patient>(),
        });
    }
}
