namespace CovidLetter.Backend.Common.Application.Barcodes.Validators;

public class BarcodeValidator<TCertificate, TSource>
{
    private readonly IEnumerable<IBarcodeValidator<TCertificate, TSource>> validators;

    public BarcodeValidator(IEnumerable<IBarcodeValidator<TCertificate, TSource>> validators)
    {
        this.validators = validators;
    }

    public ValidationResult Validate(Barcode<TCertificate> barcode, TSource source)
    {
        foreach (var validator in this.validators)
        {
            var isValid = validator.Validate(barcode, source);
            if (!isValid.Success)
            {
                return isValid;
            }
        }

        return ValidationResult.Successful();
    }

    public void Validate(
        IEnumerable<Barcode<TCertificate>> barcodes,
        TSource source)
    {
        foreach (var barcode in barcodes)
        {
            var isValid = this.Validate(barcode, source);
            if (!isValid.Success)
            {
                throw new InvalidOperationException("A barcode is not valid: " + isValid.FailureReason);
            }
        }
    }
}
