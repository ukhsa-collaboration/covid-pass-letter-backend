namespace CovidLetter.Backend.Processing.Functions;

using System.Globalization;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Infrastructure.AzureFiles;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class CleanupJsonFilesFunction
{
    private readonly ILogger<CleanupJsonFilesFunction> log;
    private readonly AzureFileSystem azureFileSystem;
    private readonly IClock clock;
    private readonly AppFunction appFunction;

    public CleanupJsonFilesFunction(
        ILogger<CleanupJsonFilesFunction> log,
        AzureFileSystem azureFileSystem,
        IClock clock,
        AppFunction appFunction)
    {
        this.azureFileSystem = azureFileSystem;
        this.clock = clock;
        this.appFunction = appFunction;
        this.log = log;
    }

    [FunctionName(AppFunction.CleanupJsonFiles)]
    public async Task RunAsync(
        [TimerTrigger($"%{AppFunction.CleanupJsonFiles}Schedule%")] TimerInfo myTimer,
        CancellationToken cancellationToken = default)
    {
        await this.appFunction.RunAsync(AppFunction.CleanupJsonFiles, async () => await this.RunAsync(cancellationToken));
    }

    private async Task RunAsync(CancellationToken token)
    {
        var longTermFileStoreDirectory = await this.azureFileSystem.GetOrCreateLongTermFileStoreOutputDirectoryAsync(token);

        foreach (var file in await longTermFileStoreDirectory.GetAllFilesRecursiveAsync().ToListAsync(token))
        {
            try
            {
                var metadata = await file.GetMetadataAsync(token);
                if (!metadata.TryGetValue(FileMetadataKeys.Processed, out var processedString))
                {
                    this.log.LogInformation("Skipped {File} because it has not been processed", file.Path);
                    continue;
                }

                var processed = DateTime.ParseExact(processedString, "O", CultureInfo.InvariantCulture);
                if (processed > this.clock.UtcNow.AddDays(-28).Date)
                {
                    this.log.LogInformation(
                        "Not deleting {File} yet because it was last processed {ProcessedOn}",
                        file.Path,
                        processed);
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
