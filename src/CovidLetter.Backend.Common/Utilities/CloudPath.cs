namespace CovidLetter.Backend.Common.Utilities;

public static class CloudPath
{
    public static string GetRelativePath(string relativeTo, string path)
    {
        return Normalize(Path.GetRelativePath(relativeTo, path));
    }

    public static IEnumerable<string> GetDirectories(string path)
    {
        var directoryName = Path.GetDirectoryName(path) ?? string.Empty;
        return string.IsNullOrEmpty(directoryName)
            ? Enumerable.Empty<string>()
            : Normalize(directoryName).Split("/");
    }

    public static string GetFileName(string path)
    {
        return Path.GetFileName(path);
    }

    public static string EncodePath(string path)
    {
        return Uri.EscapeDataString(path);
    }

    private static string Normalize(string? path)
    {
        return path?.Replace("\\", "/") ?? string.Empty;
    }
}
