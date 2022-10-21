namespace CovidLetter.Backend.Common.Utilities;

using NodaTime;

public interface IClock
{
    DateTime UtcNow { get; }

    Instant GetCurrentInstant();

    bool IsOutsideSociableHours();

    DateTimeOffset GetNextTimeInsideSociableHours();
}
