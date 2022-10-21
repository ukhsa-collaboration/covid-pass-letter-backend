namespace CovidLetter.Backend.Common.Utilities;

using System.Diagnostics.CodeAnalysis;

public static class DictionaryExtensions
{
    public static bool TryGetStringValue(
        this IDictionary<string, object> userProperties,
        string key,
        [NotNullWhen(true)] out string? checksum)
    {
        if (userProperties.TryGetValue(key, out var value))
        {
            checksum = (string)value;
            return true;
        }

        checksum = null;
        return false;
    }
}
