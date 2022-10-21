namespace CovidLetter.Backend.Common.Application.Barcodes;

using System.IO.Compression;
using CovidLetter.Backend.Common.Application.Cbor;
using CovidLetter.Backend.Common.Application.Encoders;

public class BarcodeDecoder : IBarcodeDecoder
{
    ICoseSign1 IBarcodeDecoder.Decode(string input) => Decode(input);

    public static ICoseSign1 Decode(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (input.Length == 0)
        {
            throw new ArgumentException($"{nameof(input)} length must be non-zero.", nameof(input));
        }

        input = input[4..];

        using var inMemoryStream = new MemoryStream(Base45Encoding.GetBytes(input));
        using var outMemoryStream = new MemoryStream();
        using var outZStream = new ZLibStream(inMemoryStream, CompressionMode.Decompress, true);
        outZStream.CopyTo(outMemoryStream);

        return new CoseSign1(outMemoryStream.ToArray());
    }
}
