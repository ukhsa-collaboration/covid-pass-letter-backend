namespace CovidLetter.Backend.Common.Application.Barcodes.Validators;

using CovidLetter.Backend.Common.Options;
using NodaTime;
using IClock = CovidLetter.Backend.Common.Utilities.IClock;

public class HealthCertificateValidator<TCertificate, TSource>
    : IBarcodeValidator<TCertificate, TSource>
{
    private readonly BarcodeOptions options;
    private readonly IClock clock;

    public HealthCertificateValidator(BarcodeOptions options, IClock clock)
    {
        this.options = options;
        this.clock = clock;
    }

    public ValidationResult Validate(Barcode<TCertificate> barcode, TSource source)
    {
        var iss = barcode.Certificate.Iss;

        var iat = Instant.FromUnixTimeSeconds(barcode.Certificate.Iat);
        var exp = Instant.FromUnixTimeSeconds(barcode.Certificate.Exp);

        if (iat.InUtc().Date != this.clock.GetCurrentInstant().InUtc().Date)
        {
            return ValidationResult.Failed("Invalid certificate: iat is not today: " + iat);
        }

        if (iat.Plus(Duration.FromDays(BarcodeConstants.DaysBarcodeAreValidFor)).InUtc().Date != exp.InUtc().Date)
        {
            return ValidationResult.Failed($"Invalid certificate: exp {exp} is not valid for iat {iat}");
        }

        if (iss != BarcodeConstants.GB)
        {
            return this.options.IncludePiiInErrors
                ? ValidationResult.Failed($"Invalid certificate: iss {iss} is not {BarcodeConstants.GB}")
                : ValidationResult.Failed("Invalid certificate: iss is not " + BarcodeConstants.GB);
        }

        return ValidationResult.Successful();
    }
}
