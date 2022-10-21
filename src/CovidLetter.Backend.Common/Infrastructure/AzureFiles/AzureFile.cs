namespace CovidLetter.Backend.Common.Infrastructure.AzureFiles;

using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Sas;
using CovidLetter.Backend.Common.Utilities;

public class AzureFile
    : IAzureNode
{
    private readonly ShareFileClient file;

    public AzureFile(ShareFileClient file)
    {
        this.file = file;
    }

    public string Path => this.file.Path;

    public string Name => this.file.Name;

    public async Task<ShareFileProperties> GetPropertiesAsync(CancellationToken cancellationToken = default)
    {
        return (await this.file.GetPropertiesAsync(cancellationToken)).Value;
    }

    public async Task<IDictionary<string, string>> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        return (await this.GetPropertiesAsync(cancellationToken)).Metadata;
    }

    public async Task SetMetadataAsync(IDictionary<string, string> metadata, CancellationToken cancellationToken = default)
    {
        await this.file.SetMetadataAsync(metadata, cancellationToken);
    }

    public async Task<Stream> DownloadAsStreamAsync(CancellationToken cancellationToken = default)
    {
        var response = await this.file.DownloadAsync(cancellationToken: cancellationToken);

        // response.Value is disposable, but all it currently does is dispose the underlying stream which we are returning
        return response.Value.Content;
    }

    public async Task<byte[]> DownloadAsByteArrayAsync(CancellationToken cancellationToken = default)
    {
        await using var streamHolder = await this.DownloadAsStreamAsync(cancellationToken);
        await using var output = new MemoryStream();
        await streamHolder.CopyToAsync(output, cancellationToken);
        return output.ToArray();
    }

    public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return await this.file.ExistsAsync(cancellationToken);
    }

    public async Task<bool> DeleteIfExistsAsync()
    {
        return (await this.file.DeleteIfExistsAsync()).Value;
    }

    public string GetPathRelativeTo(string relativeTo)
    {
        return CloudPath.GetRelativePath(relativeTo, this.Path);
    }
}
