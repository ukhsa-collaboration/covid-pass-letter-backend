namespace CovidLetter.Backend.Common.Application.Constants;

public static class FileMetadataKeys
{
    /// <summary>
    ///     Key for the "FileType" metadata flag on each file.
    /// </summary>
    public const string FileType = $"{StringConsts.PublicPrefix}file_type";

    /// <summary>
    ///     Key for the "processed" metadata flag on each file.
    /// </summary>
    public const string Processed = $"{StringConsts.PublicPrefix}processed";

    /// <summary>
    ///     Key for the "sha256_checksum" metadata flag on each file.
    /// </summary>
    public const string Sha256Checksum = $"{StringConsts.PublicPrefix}sha256_checksum";

    /// <summary>
    ///     Key for the "uploaded" metadata flag on each file.
    /// </summary>
    public const string Uploaded = $"{StringConsts.PublicPrefix}uploaded";
}
