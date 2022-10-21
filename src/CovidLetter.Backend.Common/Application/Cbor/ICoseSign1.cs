namespace CovidLetter.Backend.Common.Application.Cbor;

public interface ICoseSign1
{
    byte[]? GetKeyIdentifier();

    bool VerifySignature(byte[] publicKey);

    string? GetJson();
}
