namespace CovidLetter.Backend.Ingress.Functions;

using System.Globalization;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Infrastructure.AzureFiles;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class CleanupIncomingCsvFilesFunction
{
    private readonly ILogger<CleanupIncomingCsvFilesFunction> log;
    private readonly AzureFileSystem azureFileSystem;
    private readonly IClock clock;
    private readonly AppFunction appFunction;

    public CleanupIncomingCsvFilesFunction(
        ILogger<CleanupIncomingCsvFilesFunction> log,
        AzureFileSystem azureFileSystem,
        IClock clock,
        AppFunction appFunction)
    {
        this.azureFileSystem = azureFileSystem;
        this.clock = clock;
        this.appFunction = appFunction;
        this.log = log;
    }

    [FunctionName(AppFunction.CsvCleanup)]
    public async Task Run([TimerTrigger("%CsvCleanupSchedule%")] TimerInfo myTimer)
    {
        await this.appFunction.RunAsync(AppFunction.CsvCleanup, async () => await this.RunAsync());
    }

    private async Task RunAsync()
    {
        var directory = await this.azureFileSystem.GetOrCreateInputDirectoryAsync();

        foreach (var file in await directory.GetAllFilesRecursiveAsync().ToListAsync())
        {
            try
            {
                var metadata = await file.GetMetadataAsync();
                if (!metadata.TryGetValue(FileMetadataKeys.Processed, out var processedString))
                {
                    this.log.LogInformation("Skipped {File} because it has not been processed", file.Path);
                    continue;
                }

                var processed = DateTime.ParseExact(processedString, "O", CultureInfo.InvariantCulture);
                if (processed > this.clock.UtcNow.AddDays(-7).Date)
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
