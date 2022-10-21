namespace CovidLetter.Backend.Common.Application.Constants;

public static class QueueMetadataKeys
{
    public static readonly string Sha256Checksum = $"{StringConsts.PublicPrefix}sha256checksum";

    public static readonly string Version = $"{StringConsts.PublicPrefix}version";
}
