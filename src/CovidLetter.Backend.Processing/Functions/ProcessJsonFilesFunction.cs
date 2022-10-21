namespace CovidLetter.Backend.Processing.Functions;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Application.Logger;
using CovidLetter.Backend.Common.Application.Serialization;
using CovidLetter.Backend.Common.Infrastructure.AzureFiles;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class ProcessJsonFilesFunction
{
    private readonly AppFunction appFunction;
    private readonly AzureFileSystem azureFileSystem;
    private readonly IClock clock;
    private readonly IFileSerializerFactory fileSerializerFactory;
    private readonly LogHelper logger;

    public ProcessJsonFilesFunction(
        ILogger<ProcessJsonFilesFunction> logger,
        AppFunction appFunction,
        AzureFileSystem azureFileSystem,
        IFileSerializerFactory fileSerializerFactory,
        IClock clock)
    {
        this.logger = new LogHelper(logger);
        this.appFunction = appFunction;
        this.azureFileSystem = azureFileSystem;
        this.fileSerializerFactory = fileSerializerFactory;
        this.clock = clock;
    }

    private enum ProcessFileResult
    {
        Success,
        AlreadyProcessed,
        InvalidChecksum,
        InvalidFileType,
        DeserializationFailure,
        OtherFailure,
    }

    private string UtcNowString => this.clock.UtcNow.ToString("O");

    [FunctionName(AppFunction.ProcessJsonFiles)]
    public Task RunAsync(
        [TimerTrigger($"%{AppFunction.ProcessJsonFiles}Schedule%")]
        TimerInfo timerInfo,
        CancellationToken cancellationToken = default)
    {
        return this.appFunction.RunAsync(
            AppFunction.ProcessJsonFiles,
            async () =>
                await this.RunAsync(cancellationToken));
    }

    private static ConcurrentDictionary<ProcessFileResult, int> BuildTotalsDictionary() =>
        new()
        {
            [ProcessFileResult.Success] = 0,
            [ProcessFileResult.AlreadyProcessed] = 0,
            [ProcessFileResult.InvalidChecksum] = 0,
            [ProcessFileResult.InvalidFileType] = 0,
            [ProcessFileResult.DeserializationFailure] = 0,
            [ProcessFileResult.OtherFailure] = 0,
        };

    private static bool FileMetadataContainsProcessedValue(
        IDictionary<string, string> metadata,
        out string? processedOn) =>
        metadata.TryGetValue(FileMetadataKeys.Processed, out processedOn);

    private static byte[] GenerateOutputBytes<TRowType>(FileSerializer<TRowType> fileSerializer, byte[] fileBytes, out int rowCount)
        where TRowType : class, new()
    {
        var letterWrappers = JsonSerializer.Deserialize<ICollection<LetterWrapper<TRowType>>>(fileBytes, JsonConfig.Default)!;
        rowCount = letterWrappers.Count;

        var delimitedContent = fileSerializer.GenerateOutput(
            letterWrappers.Select(x => x.Letter).ToImmutableList());

        return Encoding.UTF8.GetBytes(delimitedContent);
    }

    private Task<byte[]> GetFileBytesAsync(
        AzureFile file,
        CancellationToken cancellationToken)
    {
        this.logger.LogDownloadingFile(file.Path);
        return file.DownloadAsByteArrayAsync(cancellationToken);
    }

    private async Task<ConfiguredCancelableAsyncEnumerable<AzureFile>> GetFilesFromLongTermStore(CancellationToken cancellationToken)
    {
        var longTermFileStoreDirectory = await this.azureFileSystem.GetOrCreateLongTermFileStoreOutputDirectoryAsync(cancellationToken);

        return longTermFileStoreDirectory
            .GetAllFilesRecursiveAsync()
            .WithCancellation(cancellationToken);
    }

    private static string ParseDelimitedFilename(string filename)
        => Path.GetFileNameWithoutExtension(filename) + ".csv";

    private async Task<ProcessFileResult> ProcessFile(AzureFile file, CancellationToken cancellationToken)
    {
        var metadata = await file.GetMetadataAsync(cancellationToken);

        if (FileMetadataContainsProcessedValue(metadata, out var processedOn))
        {
            this.logger.LogFileAlreadyProcessed(file.Path, processedOn!);
            return ProcessFileResult.AlreadyProcessed;
        }

        if (!TryGetFileTypeFromMetaData(metadata, out var fileType))
        {
            this.logger.LogSkipFileInvalidFileType(file.Path);
            return ProcessFileResult.InvalidFileType;
        }

        if (!TryGetChecksumFromMetaData(metadata, out var checksum))
        {
            this.logger.LogSkipFileMissingChecksum(file.Path);
            return ProcessFileResult.InvalidChecksum;
        }

        var fileBytes = await this.GetFileBytesAsync(file, cancellationToken);

        if (!ValidateChecksum(fileBytes, checksum!))
        {
            this.logger.LogSkipFileInvalidChecksum(file.Path);
            return ProcessFileResult.InvalidChecksum;
        }

        if (!this.TryGenerateOutputBytes(fileType, file.Name, fileBytes, out var outputBytes, out var rowCount))
        {
            return ProcessFileResult.DeserializationFailure;
        }

        var delimitedFileName = ParseDelimitedFilename(file.Name);

        await this.WriteDelimitedFileToStorage(
            delimitedFileName,
            outputBytes,
            cancellationToken);

        this.logger.LogFileCreated(delimitedFileName, rowCount);

        await this.SetProcessedMetadataValue(file, metadata, cancellationToken);

        return ProcessFileResult.Success;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var filesAsyncEnumerable = await this.GetFilesFromLongTermStore(cancellationToken);

        var totals = BuildTotalsDictionary();

        await foreach (var file in filesAsyncEnumerable)
        {
            try
            {
                var result = await this.ProcessFile(file, cancellationToken);
                totals[result]++;
            }
            catch (Exception ex)
            {
                this.logger.LogError(AppEventId.ErrorConvertingJsonToCSV, ex, $"Unhandled error while processing {file.Name}");
                totals[ProcessFileResult.OtherFailure]++;
            }
        }

        this.logger.LogTotals(totals);
    }

    private Task SetProcessedMetadataValue(
        AzureFile file,
        IDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        metadata[FileMetadataKeys.Processed] = this.UtcNowString;
        return file.SetMetadataAsync(metadata, cancellationToken);
    }

    private Func<byte[], (byte[] OutputBytes, int RowCount)> GetOutputGeneratorForFileType(FileType fileType)
    {
        switch (fileType)
        {
            case FileType.SuccessLetterGb:
            case FileType.SuccessSpecialPrintLetterGb:
            case FileType.SuccessLetterIm:
            case FileType.SuccessSpecialPrintLetterIm:
            case FileType.SuccessLetterWales:
            case FileType.SuccessSpecialPrintLetterWales:
                return inputBytes =>
                {
                    (byte[] OutputBytes, int RowCount) result;
                    result.OutputBytes = GenerateOutputBytes(
                        this.fileSerializerFactory.CreateFileSerializer<Letter>(),
                        inputBytes,
                        out result.RowCount);
                    return result;
                };

            case FileType.FailureLetterGb:
            case FileType.FailureSpecialPrintLetterGb:
            case FileType.FailureLetterIm:
            case FileType.FailureSpecialPrintLetterIm:
            case FileType.FailureLetterWales:
            case FileType.FailureSpecialPrintLetterWales:
                return inputBytes =>
                {
                    (byte[] OutputBytes, int RowCount) result;
                    result.OutputBytes = GenerateOutputBytes(
                        this.fileSerializerFactory.CreateFileSerializer<FailureLetter>(),
                        inputBytes,
                        out result.RowCount);
                    return result;
                };

            default:
                throw new ArgumentException($"Invalid file type: {fileType}", nameof(fileType));
        }
    }

    private bool TryGenerateOutputBytes(FileType fileType, string fileName, byte[] fileBytes, out byte[] outputBytes, out int rowCount)
    {
        try
        {
            var outputGenerator = this.GetOutputGeneratorForFileType(fileType);
            (outputBytes, rowCount) = outputGenerator(fileBytes);
        }
        catch (Exception ex)
        {
            outputBytes = Array.Empty<byte>();
            rowCount = default;
            this.logger.LogJsonParseFailed(ex, fileName);
            return false;
        }

        return true;
    }

    private static bool TryGetChecksumFromMetaData(
        IDictionary<string, string> metadata,
        out string? checksum) =>
        metadata.TryGetValue(FileMetadataKeys.Sha256Checksum, out checksum);

    private static bool TryGetFileTypeFromMetaData(
        IDictionary<string, string> metadata,
        out FileType fileType)
    {
        fileType = default;
        return
            metadata.TryGetValue(FileMetadataKeys.FileType, out var fileTypeStr)
            && Enum.TryParse(fileTypeStr, out fileType);
    }

    private static bool ValidateChecksum(
        byte[] fileBytes,
        string checksum) =>
        checksum.Equals(Checksum.Sha256(fileBytes));

    private async Task WriteDelimitedFileToStorage(string fileName, byte[] bytes, CancellationToken cancellationToken)
    {
        var outputDirectory = await this.azureFileSystem.GetOrCreateDelimitedFileOutputDirectoryAsync(cancellationToken);

        await outputDirectory.CreateAndUploadFileAsync(
            fileName,
            bytes,
            new Dictionary<string, string>
            {
                [FileMetadataKeys.Sha256Checksum] = Checksum.Sha256(bytes),
            },
            cancellationToken);
    }

    private class LogHelper
    {
        private readonly ILogger<ProcessJsonFilesFunction> logger;

        public LogHelper(ILogger<ProcessJsonFilesFunction> logger)
        {
            this.logger = logger;
        }

        public void LogDownloadingFile(string filePath)
            => this.logger.LogInformation("Downloading {File}", filePath);

        public void LogError(EventId eventId, Exception? exception, string? message, params object?[] args) =>
            this.logger.LogError(eventId, exception, message, args);

        public void LogFileAlreadyProcessed(string filePath, string processedOn)
            => this.logger.LogDebug(
                "Skipping {File} because it was already processed on {ProcessedOn}",
                filePath,
                processedOn);

        public void LogFileCreated(string delimitedFileName, int letterWrapperCount)
            => this.logger.LogInformation(
                "Created {File} with {Count} rows",
                delimitedFileName,
                letterWrapperCount);

        public void LogJsonParseFailed(Exception exception, string fileName)
            => this.logger.LogWarning(
                exception,
                "Failed to parse JSON from {File}.",
                fileName);

        public void LogSkipFileInvalidChecksum(string filePath)
            => this.logger.LogWarning("Skipping {File} due to invalid checksum.", filePath);

        public void LogSkipFileInvalidFileType(string filePath)
            => this.logger.LogWarning("Skipping {File} due to invalid filetype metadata.", filePath);

        public void LogSkipFileMissingChecksum(string filePath)
            => this.logger.LogWarning("Skipping {File} due to missing checksum metadata.", filePath);

        public void LogTotals(IDictionary<ProcessFileResult, int> totals)
            => this.logger.LogInformation(
                "Finished processing JSON files: {success} successful, {alreadyProcessed} already processed, {invalidChecksum} invalid checksums, {invalidFileType} invalid file types, {deserializationFailure} deserialization failures, {otherFailure} unknown failures.",
                totals[ProcessFileResult.Success],
                totals[ProcessFileResult.AlreadyProcessed],
                totals[ProcessFileResult.InvalidChecksum],
                totals[ProcessFileResult.InvalidFileType],
                totals[ProcessFileResult.DeserializationFailure],
                totals[ProcessFileResult.OtherFailure]);
    }
}
