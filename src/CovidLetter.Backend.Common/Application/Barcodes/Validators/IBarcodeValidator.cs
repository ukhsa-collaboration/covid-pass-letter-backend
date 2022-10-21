namespace CovidLetter.Backend.Common.Application.Barcodes.Validators;

using CovidLetter.Backend.Common.Application.Cbor;
using CovidLetter.Backend.Common.Infrastructure.Barcodes;

public interface IBarcodeValidator<TCertificate, in TSource>
{
    ValidationResult Validate(Barcode<TCertificate> barcode, TSource source);
}

public record Barcode<T>(string Id, ICoseSign1 CoseSign1, HealthCertificate<T> Certificate);
