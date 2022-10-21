namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using NodaTime;

public class PostgresLetterRequestStore
{
    private readonly PostgresRunner runner;
    private readonly IChaosMonkey chaosMonkey;
    private readonly FeatureToggle featureToggle;

    public PostgresLetterRequestStore(
        PostgresRunner runner,
        IChaosMonkey chaosMonkey,
        FeatureToggle featureToggle)
    {
        this.runner = runner;
        this.chaosMonkey = chaosMonkey;
        this.featureToggle = featureToggle;
    }

    public async Task AddAsync(LetterWrapper<Letter> letter)
    {
        await this.runner.RunAsync(nameof(this.AddAsync), async database =>
        {
            await database.ExecuteAsync(
                @"
INSERT INTO letter_request (created_on, unique_hash)
VALUES (@createdOn, @uniquehash)",
                new LetterRequestEntity
                {
                    CreatedOn = Instant.FromDateTimeUtc(letter.CreatedOn),
                    UniqueHash = letter.Letter.GetNhsDobHash(),
                });
        });
    }

    public Task<bool> AddRequestIfNoneInExclusionPeriodAsync(
        LetterRequest request,
        Instant startOfExclusionPeriod,
        Instant dateOfThisRequest)
    {
        var bypassExclusionPeriod = this.featureToggle.IsEnabled(FeatureToggle.BypassExclusionPeriod);

        return this.runner.RunSerializableAsync(
            nameof(this.AddRequestIfNoneInExclusionPeriodAsync),
            async database =>
            {
                var lastCreatedOn = await database.ExecuteScalarAsync<Instant?>(
                    @"
SELECT created_on
FROM letter_request
WHERE unique_hash = @uniqueHash
  AND created_on > @start
LIMIT 1",
                    new { uniqueHash = request.GetNhsDobHash(), start = startOfExclusionPeriod });

                this.chaosMonkey.Poke();

                if (!bypassExclusionPeriod && lastCreatedOn > startOfExclusionPeriod)
                {
                    return false;
                }

                await database.ExecuteAsync(
                    @"
INSERT INTO letter_request (created_on, unique_hash)
VALUES (@createdOn, @uniqueHash)",
                    new LetterRequestEntity
                    {
                        CreatedOn = dateOfThisRequest,
                        UniqueHash = request.GetNhsDobHash(),
                    });
                return true;
            });
    }

    public async Task DeleteRequestsOlderThan(Instant lastRetentionDate)
    {
        await this.runner.RunAsync(nameof(this.DeleteRequestsOlderThan), async database =>
        {
            await database.ExecuteAsync(
                @"
DELETE FROM letter_request 
WHERE created_on < @lastRetentionDate",
                new { lastRetentionDate });
        });
    }
}
