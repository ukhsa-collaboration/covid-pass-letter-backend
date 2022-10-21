namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

using NodaTime;

public class DestinationFileEntity
{
    public Guid Id { get; set; }

    public int FileType { get; set; }

    public Instant CreatedOn { get; set; }

    public string Name { get; set; } = null!;

    public Instant? SentOn { get; set; }
}
