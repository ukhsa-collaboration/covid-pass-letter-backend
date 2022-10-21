namespace CovidLetter.Backend.Common.Application.BankHolidays;

using System.Reflection;
using System.Text.Json;
using CovidLetter.Backend.Common.Application.Logger;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class ResourceFileBankHolidayService : IBankHolidayService
{
    public const string BankHolidaysJsonFileName = "bank-holidays.json";
    private readonly IClock clock;

    private readonly ILogger<ResourceFileBankHolidayService> logger;
    private readonly ResourceFileBankHolidayServiceOptions options;

    private ICollection<DateTime>? bankHolidayDates;

    public ResourceFileBankHolidayService(
        ILogger<ResourceFileBankHolidayService> logger,
        IClock clock,
        IOptions<ResourceFileBankHolidayServiceOptions> options)
    {
        this.logger = logger;
        this.clock = clock;
        this.options = options.Value;
    }

    private DateTime Today =>
        this.options.BankHolidayTodayDateOverride?.Date
        ?? this.clock.UtcNow.Date;

    public bool IsBankHoliday() => this.IsBankHoliday(this.Today);

    public bool IsBankHoliday(DateTime date)
    {
        if (this.bankHolidayDates is not null)
        {
            return this.bankHolidayDates.Contains(date.Date);
        }

        try
        {
            this.bankHolidayDates = ReadBankHolidaysFromEmbeddedResource();
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                AppEventId.ErrorReadingBankHolidayEmbeddedResource,
                ex,
                "Failed to read bank holiday dates");

            return false;
        }

        return this.bankHolidayDates.Contains(date.Date);
    }

    private static List<DateTime> ReadBankHolidaysFromEmbeddedResource()
    {
        /* Note that the embedded resource file available locally is a sample.
           This file is auto-generated from a gov.uk API in the build pipeline
           using powershell script /assets/get-bank-holidays.ps1 */

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.{BankHolidaysJsonFileName}";

        using var manifestResourceStream = assembly.GetManifestResourceStream(resourceName)
                                           ?? throw new IOException($"Unable to read embedded resource: {resourceName}");

        return JsonSerializer.Deserialize<List<DateTime>>(manifestResourceStream, JsonConfig.Default)
               ?? throw new JsonException($"Unable to deserialize embedded resource: {resourceName}");
    }
}
