namespace CovidLetter.Backend.Common.Application.Barcodes;

using System.Text.Json;
using CovidLetter.Backend.Common.Application.Barcodes.Validators;
using CovidLetter.Backend.Common.Infrastructure.Barcodes;

public class BarcodeConverter
{
    private readonly IBarcodeDecoder decoder;

    public BarcodeConverter(IBarcodeDecoder decoder)
    {
        this.decoder = decoder;
    }

    public Barcode<TCertificate> ConvertToBarcode<TCertificate>(string? qrCode)
    {
        if (string.IsNullOrWhiteSpace(qrCode))
        {
            return default!;
        }

        var coseSign1 = this.decoder.Decode(qrCode);

        var json = coseSign1.GetJson();

        var certificate = !string.IsNullOrWhiteSpace(json)
            ? JsonSerializer.Deserialize<HealthCertificate<TCertificate>>(json)
            : null;

        if (certificate == null)
        {
            throw new InvalidOperationException("A barcode is not valid: no JSON");
        }

        return new Barcode<TCertificate>(string.Empty, coseSign1, certificate);
    }
}
