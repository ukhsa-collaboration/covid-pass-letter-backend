namespace CovidLetter.Backend.Processing.Functions;

using CovidLetter.Backend.Common.Infrastructure.Postgres;
using CovidLetter.Backend.Common.Options;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Duration = NodaTime.Duration;
using IClock = CovidLetter.Backend.Common.Utilities.IClock;

public class LetterDatabseCleanupFunction
{
    private readonly AppFunction appFunction;
    private readonly PostgresLetterStore letterStore;
    private readonly IClock clock;
    private readonly ILogger<LetterDatabseCleanupFunction> log;

    public LetterDatabseCleanupFunction(
        ILogger<LetterDatabseCleanupFunction> log,
        AppFunction appFunction,
        PostgresLetterStore letterStore,
        IClock clock)
    {
        this.log = log;
        this.appFunction = appFunction;
        this.letterStore = letterStore;
        this.clock = clock;
    }

    [FunctionName(AppFunction.LetterDatabaseCleanup)]
    public async Task RunAsync(
        [TimerTrigger($"%{AppFunction.LetterDatabaseCleanup}Schedule%")]
        TimerInfo myTimer,
        CancellationToken cancellationToken = default)
    {
        await this.appFunction.RunAsync(AppFunction.LetterDatabaseCleanup, async () => await this.RunAsync(cancellationToken));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var twentyEightDaysAgo = this.clock.GetCurrentInstant() - Duration.FromDays(28);

        this.log.LogInformation("Deleting letters older than {RetentionLimit}", twentyEightDaysAgo);

        await this.letterStore.DeleteLettersAndDestinationFilesOlderThan(twentyEightDaysAgo);
    }
}
