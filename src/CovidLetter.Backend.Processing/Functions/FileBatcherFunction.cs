namespace CovidLetter.Backend.Processing.Functions;

using System.Text;
using System.Text.Json;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Application.Logger;
using CovidLetter.Backend.Common.Infrastructure.AzureFiles;
using CovidLetter.Backend.Common.Infrastructure.Postgres;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NodaTime;
using IClock = CovidLetter.Backend.Common.Utilities.IClock;

public class FileBatcherFunction
{
    private readonly ILogger<FileBatcherFunction> log;
    private readonly PostgresLetterStore letterStore;
    private readonly AppFunction appFunction;
    private readonly AzureFileSystem azureFileSystem;
    private readonly IClock clock;
    private readonly IChaosMonkey chaosMonkey;

    public FileBatcherFunction(
        ILogger<FileBatcherFunction> log,
        PostgresLetterStore letterStore,
        AppFunction appFunction,
        AzureFileSystem azureFileSystem,
        IClock clock,
        IChaosMonkey chaosMonkey)
    {
        this.log = log;
        this.letterStore = letterStore;
        this.appFunction = appFunction;
        this.azureFileSystem = azureFileSystem;
        this.clock = clock;
        this.chaosMonkey = chaosMonkey;
    }

    [FunctionName(AppFunction.FileBatcher)]
    public async Task Run([TimerTrigger("%FileBatcherSchedule%")] TimerInfo myTimer)
    {
        await this.appFunction.RunAsync(AppFunction.FileBatcher, async () => await this.RunAsync(), false);
    }

    private async Task RunAsync()
    {
        foreach (var file in await this.letterStore.GetUnsentFilesAsync())
        {
            try
            {
                await this.CleanupFileAsync(file);
            }
            catch (Exception ex)
            {
                this.log.LogError(AppEventId.ErrorCleaningUpPreviousBatches, ex, "Unhandled error when cleaning unsent {File}", file.Name);
            }
        }

        var now = this.clock.GetCurrentInstant();
        var outputDirectory = await this.azureFileSystem.GetOrCreateLongTermFileStoreOutputDirectoryAsync();

        var batches = await this.letterStore.BatchLettersAsync(now);

        if (!batches.Any())
        {
            this.log.LogInformation("No Letters found to send");
            return;
        }

        foreach (var batch in batches)
        {
            try
            {
                await this.SendBatchAsync(batch, outputDirectory);
            }
            catch (Exception ex)
            {
                this.log.LogError(AppEventId.ErrorCreatingBatch, ex, "Unhandled error when sending {File}", batch.Name);
            }
        }
    }

    private async Task CleanupFileAsync(DestinationFileEntity file)
    {
        var outputDirectory = await this.azureFileSystem.GetOrCreateLongTermFileStoreOutputDirectoryAsync();
        var fileClient = outputDirectory.GetFile(file.Name);

        this.chaosMonkey.Poke("cleanup");

        if (!await fileClient.ExistsAsync())
        {
            await this.letterStore.AbortFile(file);
            this.log.LogInformation("Aborted unsent file {File}", file.Name);
            return;
        }

        var properties = await fileClient.GetPropertiesAsync();
        var actualChecksum = Checksum.Sha256(await fileClient.DownloadAsByteArrayAsync());
        if (properties.Metadata.TryGetValue(FileMetadataKeys.Sha256Checksum, out var checksum) &&
            checksum == actualChecksum)
        {
            // File was uploaded, so nothing else to do. Mark it as complete!
            file.SentOn = properties.SmbProperties.FileCreatedOn != null
                ? Instant.FromDateTimeOffset(properties.SmbProperties.FileCreatedOn.Value)
                : this.clock.GetCurrentInstant();
            await this.letterStore.MarkSentAsync(file);
            this.log.LogInformation("Completed previously sent file {File}", file.Name);
        }
        else
        {
            // File was unsuccessfully uploaded, so delete it and try again
            await fileClient.DeleteIfExistsAsync();
            await this.letterStore.AbortFile(file);
            this.log.LogInformation("Deleted and aborted unsent {File}", file.Name);
        }
    }

    private async Task SendBatchAsync(DestinationFileEntity batch, AzureDirectory outputDirectory)
    {
        this.chaosMonkey.Poke("before_upload");

        byte[] fileContentToWrite;
        int letterCount;

        if (BatchIsForFailureFile(batch))
        {
            (fileContentToWrite, letterCount) = await this.GetFileContentAndLetterCountForLetterType<FailureLetter>(batch);
        }
        else
        {
            (fileContentToWrite, letterCount) = await this.GetFileContentAndLetterCountForLetterType<Letter>(batch);
        }

        await outputDirectory.CreateAndUploadFileAsync(batch.Name, fileContentToWrite, new Dictionary<string, string>
        {
            [FileMetadataKeys.Sha256Checksum] = Checksum.Sha256(fileContentToWrite),
            [FileMetadataKeys.FileType] = batch.FileType.ToString(),
        });

        this.chaosMonkey.Poke("after_upload");

        this.log.LogInformation(
            "Created {File} with {Count} rows",
            batch.Name,
            letterCount);

        batch.SentOn = this.clock.GetCurrentInstant();
        await this.letterStore.MarkSentAsync(batch);

        this.log.LogInformation("Confirmed {File} was sent", batch.Name);
    }

    private async Task<(byte[] FileContent, int LetterCount)> GetFileContentAndLetterCountForLetterType<TLetter>(DestinationFileEntity batch)
    {
        var letters = await this.letterStore.GetLettersInFile<TLetter>(batch);

        var content = JsonSerializer.Serialize(letters, JsonConfig.Default);
        var bytes = Encoding.UTF8.GetBytes(content);

        return (bytes, letters.Count);
    }

    private static bool BatchIsForFailureFile(DestinationFileEntity batch)
    {
        return FileNameTemplate.For((FileType)batch.FileType).IsFailureFile;
    }
}
