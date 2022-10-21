namespace CovidLetter.Backend.Common.Infrastructure.AzureFiles;

using Azure.Storage.Files.Shares;
using CovidLetter.Backend.Common.Options;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

public class AzureFileSystem
{
    private readonly IAzureClientFactory<ShareServiceClient> shareServiceClientFactory;
    private readonly StorageOptions storageOptions;

    public AzureFileSystem(
        IAzureClientFactory<ShareServiceClient> shareServiceClientFactory,
        IOptions<StorageOptions> storageOptions)
    {
        this.shareServiceClientFactory = shareServiceClientFactory;
        this.storageOptions = storageOptions.Value;
    }

    public static string InputStorage => nameof(InputStorage);

    public static string OutputStorage => nameof(OutputStorage);

    public ShareClient GetInputShareClient()
    {
        return this.GetShareServiceClient(InputStorage).GetShareClient(this.storageOptions.InputFileShare);
    }

    public async Task<AzureDirectory> GetOrCreateInputDirectoryAsync(
        CancellationToken cancellationToken = default)
    {
        return await this.GetOrCreateDirectoryAsync(
            InputStorage,
            this.storageOptions.InputFileShare,
            this.storageOptions.InputDirectory,
            cancellationToken);
    }

    public async Task<AzureDirectory> GetOrCreateLongTermFileStoreOutputDirectoryAsync(
        CancellationToken cancellationToken = default)
    {
        return await this.GetOrCreateDirectoryAsync(
            OutputStorage,
            this.storageOptions.OutputFileShare,
            this.storageOptions.LongTermFileStoreOutputDirectory,
            cancellationToken);
    }

    public async Task<AzureDirectory> GetOrCreateDelimitedFileOutputDirectoryAsync(
        CancellationToken cancellationToken = default)
    {
        return await this.GetOrCreateDirectoryAsync(
            OutputStorage,
            this.storageOptions.OutputFileShare,
            this.storageOptions.DelimitedFileOutputDirectory,
            cancellationToken);
    }

    private async Task<AzureDirectory> GetOrCreateDirectoryAsync(
        string clientName,
        string fileShare,
        string? directory,
        CancellationToken cancellationToken)
    {
        var shareServiceClient = this.GetShareServiceClient(clientName).GetShareClient(fileShare);
        if (string.IsNullOrEmpty(directory))
        {
            return new AzureDirectory(shareServiceClient.GetRootDirectoryClient());
        }

        var shareClientDirectory = shareServiceClient.GetDirectoryClient(directory);
        await AzureDirectory.CreateIfNotExistsAsync(shareClientDirectory, cancellationToken);
        return new AzureDirectory(shareClientDirectory);
    }

    private ShareServiceClient GetShareServiceClient(string name) =>
        this.shareServiceClientFactory.CreateClient(name) ??
        throw new InvalidOperationException($"Client {name} has not been registered");
}
