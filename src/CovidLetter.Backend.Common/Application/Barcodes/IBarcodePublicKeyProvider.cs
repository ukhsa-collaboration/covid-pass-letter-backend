namespace CovidLetter.Backend.Common.Application.Barcodes;

public interface IBarcodePublicKeyProvider
{
    Task<IReadOnlyDictionary<string, string>> GetKeysAsync();
}
