namespace CovidLetter.Backend.Common.Utilities;

using System.Security.Cryptography;
using System.Text;

public static class Hash
{
    public static string GetHashValue(string nhsNumber, DateTime dateOfBirth)
    {
        if (string.IsNullOrEmpty(nhsNumber) || dateOfBirth == default)
        {
            return string.Empty;
        }

        return $"{nhsNumber.ToLower()}{dateOfBirth:yyyyMMdd}".GetHashString();
    }

    private static string GetHashString(this string inputString)
    {
        var sb = new StringBuilder();
        foreach (var b in GetHash(inputString))
        {
            sb.Append($"{b:X2}");
        }

        return sb.ToString();
    }

    private static IEnumerable<byte> GetHash(string inputString)
    {
        var inputByte = Encoding.Unicode.GetBytes(inputString);
        return SHA256.HashData(inputByte);
    }
}
