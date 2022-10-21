namespace CovidLetter.Backend.Common.Application.Barcodes.Validators;

using NodaTime;

public class ChecksExpIsInFutureCertificateValidator<TCertificate, TSource>
    : IBarcodeValidator<TCertificate, TSource>
{
    public ValidationResult Validate(Barcode<TCertificate> barcode, TSource source)
    {
        var iat = Instant.FromUnixTimeSeconds(barcode.Certificate.Iat);
        var exp = Instant.FromUnixTimeSeconds(barcode.Certificate.Exp);

        if (exp <= iat)
        {
            return ValidationResult.Failed("Invalid certificate: exp is not not greater than iat");
        }

        return ValidationResult.Successful();
    }
}
