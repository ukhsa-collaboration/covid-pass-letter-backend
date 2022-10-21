namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

using NodaTime;

public class LetterEntity
{
    public Guid Id { get; set; }

    public string AppId { get; set; } = null!;

    public int FileType { get; set; }

    public Instant CreatedOn { get; set; }

    public JsonField Letter { get; set; } = null!;
}
