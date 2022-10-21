namespace CovidLetter.Backend.Common.Application.Barcodes.Validators;

public class SignatureValidator<TCert, TSource>
    : IBarcodeValidator<TCert, TSource>
{
    private readonly IReadOnlyDictionary<string, string> publicKeys;

    public SignatureValidator(IReadOnlyDictionary<string, string> publicKeys)
    {
        this.publicKeys = publicKeys;
    }

    public ValidationResult Validate(Barcode<TCert> barcode, TSource source)
    {
        var keyIdentifier = barcode.CoseSign1.GetKeyIdentifier();

        if (keyIdentifier == null || keyIdentifier.Length == 0)
        {
            return ValidationResult.Failed($"No KID found ({keyIdentifier})");
        }

        var friendlyKey = Convert.ToBase64String(keyIdentifier);
        if (!this.publicKeys.TryGetValue(friendlyKey, out var publicKey))
        {
            return ValidationResult.Failed($"Public key not found for {friendlyKey}");
        }

        if (!barcode.CoseSign1.VerifySignature(Convert.FromBase64String(publicKey)))
        {
            return ValidationResult.Failed("Invalid signature");
        }

        return ValidationResult.Successful();
    }
}
