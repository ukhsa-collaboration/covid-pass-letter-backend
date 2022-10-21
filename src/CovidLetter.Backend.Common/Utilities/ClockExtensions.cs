namespace CovidLetter.Backend.Common.Utilities;

public static class ClockExtensions
{
    public static int GetAge(this IClock clock, DateTime dateOfBirth)
    {
        var nowConcat = int.Parse(clock.UtcNow.ToString("yyyyMMdd"));
        var dobConcat = int.Parse(dateOfBirth.ToString("yyyyMMdd"));
        return (nowConcat - dobConcat) / 10000;
    }
}
