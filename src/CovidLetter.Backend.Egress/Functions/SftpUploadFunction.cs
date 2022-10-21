namespace CovidLetter.Backend.Egress.Functions;

using System.Threading.Tasks;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Application.Logger;
using CovidLetter.Backend.Common.Infrastructure.AzureFiles;
using CovidLetter.Backend.Common.Infrastructure.SftpFiles;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class SftpUploadFunction
{
    private readonly ILogger<SftpUploadFunction> log;
    private readonly AppFunction appFunction;
    private readonly SftpFileSystem sftpFileSystem;
    private readonly AzureFileSystem azureFileSystem;
    private readonly IChaosMonkey chaosMonkey;
    private readonly IClock clock;

    public SftpUploadFunction(
        ILogger<SftpUploadFunction> log,
        AppFunction appFunction,
        SftpFileSystem sftpFileSystem,
        AzureFileSystem azureFileSystem,
        IChaosMonkey chaosMonkey,
        IClock clock)
    {
        this.log = log;
        this.appFunction = appFunction;
        this.sftpFileSystem = sftpFileSystem;
        this.azureFileSystem = azureFileSystem;
        this.chaosMonkey = chaosMonkey;
        this.clock = clock;
    }

    [FunctionName(AppFunction.SftpUpload)]
    public async Task Run([TimerTrigger("%SftpUploadSchedule%")] TimerInfo myTimer)
    {
        await this.appFunction.RunAsync(AppFunction.SftpUpload, async () => await this.RunAsync());
    }

    private async Task RunAsync()
    {
        var outputDirectory = await this.azureFileSystem.GetOrCreateDelimitedFileOutputDirectoryAsync();
        this.log.LogInformation("Uploading files in {Path}...", outputDirectory.Path);

        var total = (uploaded: 0, skipped: 0, failed: 0);

        foreach (var file in await outputDirectory.GetAllFilesRecursiveAsync().ToListAsync())
        {
            try
            {
                if (await this.UploadAsync(outputDirectory, file))
                {
                    total.uploaded++;
                }
                else
                {
                    total.skipped++;
                }
            }
            catch (Exception ex)
            {
                this.log.LogError(AppEventId.ErrorSendingSftp, ex, "Unhandled error when uploading {File}", file.Path);
                total.failed++;
            }
        }

        this.log.LogInformation(
            "Finished uploading; {UploadedCount} uploaded, {SkippedCount} skipped, {FailedCount} failed",
            total.uploaded,
            total.skipped,
            total.failed);
    }

    private async Task<bool> UploadAsync(AzureDirectory directory, AzureFile file)
    {
        var metadata = await file.GetMetadataAsync();

        if (metadata.TryGetValue(FileMetadataKeys.Uploaded, out var lastUploaded))
        {
            this.log.LogInformation(
                "Skipping {File} since it was already uploaded on {UploadOn}",
                file.Path,
                lastUploaded);
            return false;
        }

        if (!metadata.TryGetValue(FileMetadataKeys.Sha256Checksum, out var expectedChecksum))
        {
            this.log.LogWarning(
                "Skipping {File} since it does not have a {Hash} metadata key",
                file.Path,
                FileMetadataKeys.Sha256Checksum);
            return false;
        }

        this.log.LogInformation("Downloading {File}", file.Path);
        var content = await file.DownloadAsByteArrayAsync();
        this.chaosMonkey.Poke();

        var actualChecksum = Checksum.Sha256(content);
        if (actualChecksum != expectedChecksum)
        {
            this.log.LogError("Expected and actual checksum do not match for {File}", file.Path);
            return false;
        }

        var relativePath = file.GetPathRelativeTo(directory.Path);
        this.sftpFileSystem.Upload(relativePath, content);

        metadata[FileMetadataKeys.Uploaded] = this.clock.UtcNow.ToString("O");
        await file.SetMetadataAsync(metadata);

        this.log.LogInformation("Uploaded {File} to {Destination}", file.Path, relativePath);
        return true;
    }
}
