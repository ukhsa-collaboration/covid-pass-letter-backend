namespace CovidLetter.Backend.Egress.Functions;

using System.Globalization;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Infrastructure.AzureFiles;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class CleanupUploadedDelimitedFilesFunction
{
    private readonly ILogger<CleanupUploadedDelimitedFilesFunction> log;
    private readonly AzureFileSystem azureFileSystem;
    private readonly IClock clock;
    private readonly AppFunction appFunction;

    public CleanupUploadedDelimitedFilesFunction(
        ILogger<CleanupUploadedDelimitedFilesFunction> log,
        AzureFileSystem azureFileSystem,
        IClock clock,
        AppFunction appFunction)
    {
        this.azureFileSystem = azureFileSystem;
        this.clock = clock;
        this.appFunction = appFunction;
        this.log = log;
    }

    [FunctionName(AppFunction.CleanupUploadedDelimitedFiles)]
    public async Task RunAsync(
        [TimerTrigger($"%{AppFunction.CleanupUploadedDelimitedFiles}Schedule%")] TimerInfo myTimer,
        CancellationToken cancellationToken = default)
    {
        await this.appFunction.RunAsync(AppFunction.CleanupUploadedDelimitedFiles, async () => await this.RunAsync(cancellationToken));
    }

    private async Task RunAsync(CancellationToken token)
    {
        var outputDirectory = await this.azureFileSystem.GetOrCreateDelimitedFileOutputDirectoryAsync(token);

        foreach (var file in await outputDirectory.GetAllFilesRecursiveAsync().ToListAsync(token))
        {
            try
            {
                var metadata = await file.GetMetadataAsync(token);
                if (!metadata.TryGetValue(FileMetadataKeys.Uploaded, out var uploadedString))
                {
                    this.log.LogInformation("Skipped {File} because it has not been uploaded", file.Path);
                    continue;
                }

                var uploadedOn = DateTime.ParseExact(uploadedString, "O", CultureInfo.InvariantCulture);
                if (uploadedOn > this.clock.UtcNow.AddDays(-28).Date)
                {
                    this.log.LogInformation(
                        "Not deleting {File} yet because it was only uploaded on {UploadOn}",
                        file.Path,
                        uploadedOn);
                }
                else
                {
                    var deleted = await file.DeleteIfExistsAsync();
                    this.log.LogInformation("Deleted {File}, with success: {Success}", file.Path, deleted);
                }
            }
            catch (Exception ex)
            {
                this.log.LogError(ex, "Failed to cleanup {File}", file.Path);
            }
        }
    }
}
