namespace CovidLetter.Backend.Common.Utilities;

using System.Security.Cryptography;

public static class Checksum
{
    public static string Sha256(byte[] content)
    {
        return Convert.ToBase64String(SHA256.HashData(content));
    }
}
