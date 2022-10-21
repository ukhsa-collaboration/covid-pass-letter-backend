namespace CovidLetter.Backend.Common.Application.Barcodes;

using CovidLetter.Backend.Common.Application.Cbor;

public interface IBarcodeDecoder
{
    ICoseSign1 Decode(string input);
}
