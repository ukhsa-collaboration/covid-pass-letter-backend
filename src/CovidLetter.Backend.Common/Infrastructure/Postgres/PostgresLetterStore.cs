namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

using System.Text.Json;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Extensions.Options;
using NodaTime;

public class LetterBatch
{
    public FileType FileType { get; set; }

    public int BatchNum { get; set; }
}

public class PostgresLetterStore
{
    private readonly PostgresRunner runner;
    private readonly FunctionOptions functionOptions;
    private readonly IGuidGenerator guidGenerator;

    public PostgresLetterStore(
        PostgresRunner runner,
        IOptions<FunctionOptions> functionOptions,
        IGuidGenerator guidGenerator)
    {
        this.runner = runner;
        this.guidGenerator = guidGenerator;
        this.functionOptions = functionOptions.Value;
    }

    public async Task<bool> AlreadyAddedAsync(string appId)
    {
        return await this.runner.RunAsync(
            nameof(this.AlreadyAddedAsync),
            async database =>
            {
                return await database.ExecuteScalarAsync<bool>(
                    @"
SELECT EXISTS (
SELECT 1
FROM letter
WHERE app_id = @appId)",
                    new { appId });
            });
    }

    public async Task AddAsync<TRow>(LetterWrapper<TRow> letter, Guid? destinationFileId = null)
    {
        await this.runner.RunSerializableAsync(nameof(this.AddAsync), async database =>
        {
            await database.ExecuteAsync(
                @"
INSERT INTO letter (id, app_id, file_type, created_on, letter)
VALUES (@id, @appId, @fileType, @createdOn, @letter)",
                new LetterEntity
                {
                    Id = letter.Id,
                    AppId = letter.AppId,
                    FileType = (int)letter.FileType,
                    CreatedOn = Instant.FromDateTimeUtc(letter.CreatedOn),
                    Letter = new JsonField(JsonSerializer.Serialize(letter.Letter, JsonConfig.Default)),
                });

            await database.ExecuteAsync(
                @"
INSERT INTO letter_destination_file (letter_id, file_type, destination_file_id)
VALUES (@letterId, @fileType, @destinationFileId)",
                new LetterDestinationFileEntity
                {
                    LetterId = letter.Id,
                    FileType = (int)letter.FileType,
                    DestinationFileId = destinationFileId,
                });
        });
    }

    public async Task<IEnumerable<DestinationFileEntity>> GetUnsentFilesAsync()
    {
        return await this.runner.RunAsync(nameof(this.GetUnsentFilesAsync), async database =>
        {
            return await database.QueryAsync<DestinationFileEntity>(@"
SELECT id, name
FROM destination_file
WHERE sent_on IS NULL");
        });
    }

    public async Task<List<DestinationFileEntity>> BatchLettersAsync(Instant now)
    {
        return await this.runner.RunSerializableAsync(nameof(this.BatchLettersAsync), async database =>
        {
            await database.ExecuteAsync(
                @"
SELECT letter_id, file_type, row_num, ((row_num - 1)/@batchSize) + 1 AS batch_num
INTO TEMPORARY letter_in_batch
FROM (
    SELECT letter_id, file_type, ROW_NUMBER() OVER (PARTITION BY file_type ORDER BY letter_id) AS row_num
    FROM letter_destination_file
    WHERE destination_file_id IS NULL
      AND file_type NOT IN (
        SELECT DISTINCT file_type
        FROM destination_file
        WHERE date_trunc('day', created_on) = date_trunc('day', @now)
    )
) AS temp",
                new
                {
                    batchSize = this.functionOptions.MaxLettersPerFile,
                    now,
                });

            var batches = await database.QueryAsync<LetterBatch>(
                @"
SELECT DISTINCT file_type, batch_num
FROM letter_in_batch");

            var result = new List<DestinationFileEntity>();

            foreach (var batchGroup in batches.GroupBy(b => b.FileType).OrderBy(b => b.Key))
            {
                var count = batchGroup.Max(b => b.BatchNum);
                if (!FileNameTemplate.TryFor(batchGroup.Key, out var template))
                {
                    continue;
                }

                foreach (var batch in batchGroup.OrderBy(b => b.BatchNum))
                {
                    var fileType = (int)batch.FileType;
                    var destination = new DestinationFileEntity
                    {
                        Id = this.guidGenerator.NewGuid(),
                        CreatedOn = now,
                        FileType = fileType,
                        Name = template.RenderFileNameWithJsonExtension(now.ToDateTimeUtc(), batch.BatchNum, count),
                    };

                    await database.ExecuteAsync(
                        @"
INSERT INTO destination_file(id, file_type, created_on, name, sent_on)
VALUES (@id, @fileType, @createdOn, @name, @sentOn)",
                        destination);

                    await database.ExecuteAsync(
                        @"
UPDATE letter_destination_file
SET destination_file_id = @fileId
WHERE letter_id IN (
    SELECT letter_id FROM letter_in_batch
    WHERE file_type = @fileType AND batch_num = @batchNum
)",
                        new { fileType, batchNum = batch.BatchNum, fileId = destination.Id });

                    result.Add(destination);
                }
            }

            return result;
        });
    }

    public async Task<List<LetterWrapper<TRow>>> GetLettersInFile<TRow>(DestinationFileEntity file)
    {
        return await this.runner.RunAsync(nameof(this.GetLettersInFile), async database =>
        {
            var letters = (await database.QueryAsync<LetterEntity>(
                    @"
SELECT l.id, l.app_id, l.file_type, l.created_on, l.letter
FROM letter l
JOIN letter_destination_file f ON l.id = f.letter_id
WHERE f.destination_file_id = @fileId
ORDER BY l.app_id",
                    new
                    {
                        fileId = file.Id,
                    }))
                .ToList();

            return letters.Select(entity => new LetterWrapper<TRow>(
                    entity.Id,
                    entity.AppId,
                    entity.CreatedOn.ToDateTimeUtc(),
                    (FileType)entity.FileType,
                    JsonSerializer.Deserialize<TRow>(entity.Letter.Json, JsonConfig.Default)!))
                .ToList();
        });
    }

    public async Task AbortFile(DestinationFileEntity file)
    {
        await this.runner.RunSerializableAsync(nameof(this.AbortFile), async database =>
        {
            await database.ExecuteAsync(
                @"
UPDATE letter_destination_file
SET destination_file_id = NULL
WHERE destination_file_id = @id",
                new { id = file.Id });

            var changed = await database.ExecuteAsync(
                @"
DELETE FROM destination_file
WHERE id = @id",
                new { id = file.Id, sentOn = file.SentOn });

            if (changed != 1)
            {
                throw new Exception("Expected to change 1 row, but changed: " + changed);
            }
        });
    }

    public async Task MarkSentAsync(DestinationFileEntity file)
    {
        await this.runner.RunSerializableAsync(nameof(this.MarkSentAsync), async database =>
        {
            var changed = await database.ExecuteAsync(
                @"
UPDATE destination_file
SET sent_on = @sentOn
WHERE id = @id",
                new { id = file.Id, sentOn = file.SentOn });

            if (changed != 1)
            {
                throw new Exception("Expected to change 1 row, but changed: " + changed);
            }
        });
    }

    public async Task DeleteLettersAndDestinationFilesOlderThan(Instant lastRetentionInstant)
    {
        await this.runner.RunSerializableAsync(
            nameof(this.DeleteLettersAndDestinationFilesOlderThan),
            async database =>
            {
                await database.ExecuteAsync(
                    @"
DELETE FROM letter_destination_file ldf
USING letter l, destination_file df
WHERE ldf.letter_id = l.id
AND df.id = ldf.destination_file_id
AND l.created_on < @lastRetentionInstant
AND ldf.destination_file_id IS NOT NULL
AND df.sent_on IS NOT NULL",
                    new { lastRetentionInstant });

                await database.ExecuteAsync(
                    @"
DELETE FROM letter AS l
WHERE l.created_on < @lastRetentionInstant
AND NOT EXISTS (SELECT letter_id FROM letter_destination_file AS ldf WHERE ldf.letter_id = l.id)",
                    new { lastRetentionInstant });

                await database.ExecuteAsync(
                    @"
DELETE from destination_file AS df
WHERE df.sent_on IS NOT NULL
AND df.created_on < @lastRetentionInstant
AND NOT EXISTS(SELECT destination_file_id FROM letter_destination_file AS ldf WHERE ldf.destination_file_id = df.id )",
                    new { lastRetentionInstant });
            });
    }
}
