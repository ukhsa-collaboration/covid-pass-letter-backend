namespace CovidLetter.Backend.Ingress.Functions;

using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Application.Logger;
using CovidLetter.Backend.Common.Infrastructure.AzureFiles;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class CopyOtherFilesFunction
{
    private readonly ILogger<CopyOtherFilesFunction> log;
    private readonly AppFunction appFunction;
    private readonly AzureFileSystem azureFileSystem;
    private readonly IClock clock;
    private readonly IChaosMonkey chaosMonkey;

    public CopyOtherFilesFunction(
        ILogger<CopyOtherFilesFunction> log,
        AppFunction appFunction,
        AzureFileSystem azureFileSystem,
        IClock clock,
        IChaosMonkey chaosMonkey)
    {
        this.appFunction = appFunction;
        this.azureFileSystem = azureFileSystem;
        this.clock = clock;
        this.chaosMonkey = chaosMonkey;
        this.log = log;
    }

    [FunctionName(AppFunction.CopyOtherFiles)]
    public async Task RunAsync(
        [TimerTrigger("%CopyOtherFilesSchedule%")]
        TimerInfo myTimer,
        CancellationToken cancellationToken = default)
    {
        await this.appFunction.RunAsync(AppFunction.CopyOtherFiles, async () => await this.RunAsync(cancellationToken));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var inputDirectory = await this.azureFileSystem.GetOrCreateInputDirectoryAsync(cancellationToken);
        this.log.LogInformation("Copying files in {Path}...", inputDirectory.Path);

        var total = (copied: 0, skipped: 0, failed: 0);
        foreach (var file in await inputDirectory.GetAllFilesRecursiveAsync().ToListAsync(cancellationToken))
        {
            try
            {
                if (await this.CopyAsync(inputDirectory, file, cancellationToken))
                {
                    total.copied++;
                }
                else
                {
                    total.skipped++;
                }
            }
            catch (Exception ex)
            {
                this.log.LogError(AppEventId.ErrorCopyingFiles, ex, "Unhandled error when copying {File}", file.Path);
                total.failed++;
            }
        }

        this.log.LogInformation(
            "Finished copying; {CopiedCount} copied, {SkippedCount} skipped, {FailedCount} failed",
            total.copied,
            total.skipped,
            total.failed);
    }

    private async Task<bool> CopyAsync(AzureDirectory directory, AzureFile file, CancellationToken cancellationToken)
    {
        if (FileNameTemplate.IsEnrichable(file.Path))
        {
            this.log.LogInformation("Skipping {File} because it should be enriched", file.Path);
            return false;
        }

        var metadata = await file.GetMetadataAsync(cancellationToken);

        if (metadata.TryGetValue(FileMetadataKeys.Processed, out var processed))
        {
            this.log.LogInformation(
                "Skipping {File} because it was already processed on {ProcessedOn}",
                file.Path,
                processed);
            return false;
        }

        this.log.LogInformation("Downloading {File}", file.Path);
        var content = await file.DownloadAsByteArrayAsync(cancellationToken);
        this.chaosMonkey.Poke();

        var outputDirectory = await this.azureFileSystem.GetOrCreateDelimitedFileOutputDirectoryAsync(cancellationToken);
        var uploadedFile = await outputDirectory.CreateAndUploadFileAsync(
            file.GetPathRelativeTo(directory.Path),
            content,
            new Dictionary<string, string>
            {
                [FileMetadataKeys.Sha256Checksum] = Checksum.Sha256(content),
            },
            cancellationToken);

        metadata[FileMetadataKeys.Processed] = this.clock.UtcNow.ToString("O");
        await file.SetMetadataAsync(metadata, cancellationToken);

        this.log.LogInformation("Copied {File} to {Destination}", file.Path, uploadedFile.Path);
        return true;
    }
}
