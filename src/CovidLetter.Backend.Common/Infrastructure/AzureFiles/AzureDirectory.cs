namespace CovidLetter.Backend.Common.Infrastructure.AzureFiles;

using Azure.Storage.Files.Shares;
using CovidLetter.Backend.Common.Utilities;

public class AzureDirectory
    : IAzureNode
{
    private readonly ShareDirectoryClient directoryClient;

    public AzureDirectory(ShareDirectoryClient directoryClient)
    {
        this.directoryClient = directoryClient;
    }

    public string Path => this.directoryClient.Path;

    public IAsyncEnumerable<AzureFile> GetAllFilesRecursiveAsync()
    {
        return this.GetAllLeafNodesAsync().OfType<AzureFile>().OrderBy(p => p.Path);
    }

    public async IAsyncEnumerable<IAzureNode> GetAllLeafNodesAsync()
    {
        var remaining = new Queue<ShareDirectoryClient>();
        remaining.Enqueue(this.directoryClient);

        while (remaining.Count > 0)
        {
            var dir = remaining.Dequeue();
            var filesAndDirectories = await dir.GetFilesAndDirectoriesAsync().ToListAsync();
            if (filesAndDirectories.Count == 0)
            {
                yield return new AzureDirectory(dir);
            }

            foreach (var item in filesAndDirectories)
            {
                if (item.IsDirectory)
                {
                    remaining.Enqueue(dir.GetSubdirectoryClient(item.Name));
                }
                else
                {
                    yield return new AzureFile(dir.GetFileClient(item.Name));
                }
            }
        }
    }

    public AzureFile GetFile(string filePath)
    {
        return new AzureFile(this.directoryClient.GetFileClient(filePath));
    }

    public async Task<AzureFile> CreateAndUploadFileAsync(
        string path,
        byte[] content,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var directory = this.directoryClient;
        foreach (var subDir in CloudPath.GetDirectories(path))
        {
            directory = directory.GetSubdirectoryClient(subDir);
            await CreateIfNotExistsAsync(directory, cancellationToken);
        }

        var file = directory.GetFileClient(CloudPath.GetFileName(path));
        await using var stream = new MemoryStream(content);
        await file.CreateAsync(stream.Length, cancellationToken: cancellationToken);
        await file.UploadAsync(stream, cancellationToken: cancellationToken);
        if (metadata != null)
        {
            await file.SetMetadataAsync(metadata, cancellationToken);
        }

        return new AzureFile(file);
    }

    public string GetPathRelativeTo(string relativeTo)
    {
        return CloudPath.GetRelativePath(relativeTo, this.Path);
    }

    /// <summary>
    /// Create a directory, if it doesn't already exist.
    /// We do a check to see if it exists first, because the underlying "CreateIfNotExists" method does a create call regardless, which
    /// returns a 409 which gets logged as an exception in application insights (even though its not an error!).
    /// </summary>
    public static async Task CreateIfNotExistsAsync(
        ShareDirectoryClient directory,
        CancellationToken cancellationToken)
    {
        if (!await directory.ExistsAsync(cancellationToken))
        {
            await directory.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }
    }
}
