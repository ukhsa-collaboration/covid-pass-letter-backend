namespace CovidLetter.Backend.Processing.Functions;

using CovidLetter.Backend.Common.Infrastructure.Postgres;
using CovidLetter.Backend.Common.Options;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NodaTime;
using IClock = CovidLetter.Backend.Common.Utilities.IClock;

public class LetterRequestDatabaseCleanupFunction
{
    private readonly AppFunction appFunction;
    private readonly PostgresLetterRequestStore letterRequestStore;
    private readonly IClock clock;
    private readonly ILogger<LetterRequestDatabaseCleanupFunction> log;

    public LetterRequestDatabaseCleanupFunction(
        ILogger<LetterRequestDatabaseCleanupFunction> log,
        AppFunction appFunction,
        PostgresLetterRequestStore letterRequestStore,
        IClock clock)
    {
        this.log = log;
        this.appFunction = appFunction;
        this.letterRequestStore = letterRequestStore;
        this.clock = clock;
    }

    [FunctionName(AppFunction.LetterRequestDatabaseCleanup)]
    public async Task RunAsync(
        [TimerTrigger($"%{AppFunction.LetterRequestDatabaseCleanup}Schedule%")]
        TimerInfo myTimer,
        CancellationToken cancellationToken = default)
    {
        await this.appFunction.RunAsync(AppFunction.LetterRequestDatabaseCleanup, async () => await this.RunAsync(cancellationToken));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var sevenDaysAgo = this.clock.GetCurrentInstant() - Duration.FromDays(7);

        this.log.LogInformation("Deleting letters requests older than {RetentionLimit}", sevenDaysAgo);

        await this.letterRequestStore.DeleteRequestsOlderThan(sevenDaysAgo);
    }
}
