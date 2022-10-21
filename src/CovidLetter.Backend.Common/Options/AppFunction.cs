namespace CovidLetter.Backend.Common.Options;

using CovidLetter.Backend.Common.Application.BankHolidays;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class AppFunction
{
    public const string BarcodeTest = "BarcodeTestFunction";
    public const string LetterRequest = "LetterRequestFunction";
    public const string LetterRequestHistory = "LetterRequestHistoryFunction";
    public const string CopyOtherFiles = "CopyOtherFilesFunction";
    public const string CsvCleanup = "CsvCleanupFunction";
    public const string EnrichRow = "EnrichRowFunction";
    public const string FileBatcher = "FileBatcherFunction";
    public const string Migration = "MigrationFunction";
    public const string ProcessJsonFiles = "ProcessJsonFilesFunction";
    public const string ResetBatch = "ResetBatchFunction";
    public const string SftpUpload = "SftpUploadFunction";
    public const string FailureNotification = "FailureNotificationFunction";
    public const string ResetHistory = "ResetHistoryFunction";
    public const string CleanupJsonFiles = "CleanupJsonFilesFunction";
    public const string CleanupUploadedDelimitedFiles = "CleanupUploadedDelimitedFilesFunction";
    public const string LetterDatabaseCleanup = "LetterDatabaseCleanupFunction";
    public const string LetterRequestDatabaseCleanup = "LetterRequestDatabaseCleanupFunction";

    private readonly Lazy<IReadOnlySet<string>> enabledFunctions;
    private readonly ILogger<AppFunction> log;
    private readonly IBankHolidayService bankHolidayService;

    public AppFunction(
        IOptions<FunctionOptions> functionOptions,
        ILogger<AppFunction> log,
        IBankHolidayService bankHolidayService)
    {
        this.log = log;
        this.bankHolidayService = bankHolidayService;
        this.enabledFunctions = new(() => NormalizeFunctionNames(functionOptions.Value.EnabledFunctions));
    }

    public async Task RunAsync(
        string functionName,
        Func<Task> action,
        bool runOnBankHolidays = true)
    {
        if (!this.enabledFunctions.Value.Contains(functionName))
        {
            this.log.LogInformation("Function {Name} is not enabled", functionName);
            return;
        }

        if (!runOnBankHolidays && this.bankHolidayService.IsBankHoliday())
        {
            this.log.LogInformation("Skipping function {Name} execution due to bank holiday.", functionName);
            return;
        }

        await action();
    }

    public async Task<T> RunAsync<T>(string functionName, Func<Task<T>> action, Func<T> disabledAction)
    {
        return await this.RunAsync(functionName, action, () => Task.FromResult(disabledAction()));
    }

    public async Task<T> RunAsync<T>(string functionName, Func<Task<T>> action, Func<Task<T>> disabledAction)
    {
        if (this.enabledFunctions.Value.Contains(functionName))
        {
            return await action();
        }
        else
        {
            this.log.LogInformation("Function {Name} is not enabled", functionName);
            return await disabledAction();
        }
    }

    private static HashSet<string> NormalizeFunctionNames(string? enabledFunctions)
    {
        return (enabledFunctions ?? string.Empty)
            .Split(",").Select(f => f.Trim())
            .ToHashSet(StringComparer.InvariantCultureIgnoreCase);
    }
}
