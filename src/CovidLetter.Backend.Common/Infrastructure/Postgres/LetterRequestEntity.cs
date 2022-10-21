namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

using NodaTime;

public class LetterRequestEntity
{
    public Instant CreatedOn { get; set; }

    public string UniqueHash { get; set; } = null!;
}
