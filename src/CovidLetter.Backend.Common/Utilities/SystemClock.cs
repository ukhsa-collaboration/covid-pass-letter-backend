namespace CovidLetter.Backend.Common.Utilities;

using CovidLetter.Backend.Common.Options;
using Microsoft.Extensions.Options;
using NodaTime;

public class SystemClock
    : IClock
{
    private readonly IOptions<ClockOptions> opts;

    public SystemClock(IOptions<ClockOptions> opts)
    {
        this.opts = opts;
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public Instant GetCurrentInstant() => NodaTime.SystemClock.Instance.GetCurrentInstant();

    public DateTimeOffset GetNextTimeInsideSociableHours()
    {
        var utcNow = this.UtcNow;
        var utcTimeNow = TimeOnly.FromDateTime(utcNow);
        var sociableTimeStartUtc = TimeOnly.Parse(this.opts.Value.SociableTimeStartUtc);
        var sociableTimeEndUtc = TimeOnly.Parse(this.opts.Value.SociableTimeEndUtc);

        return utcTimeNow < sociableTimeStartUtc
            ? utcNow.Date.Add(sociableTimeStartUtc.ToTimeSpan())
            : utcTimeNow >= sociableTimeEndUtc
                ? utcNow.Date.AddDays(1).Add(sociableTimeStartUtc.ToTimeSpan())
                : utcNow;
    }

    public bool IsOutsideSociableHours()
    {
        var utcTimeNow = TimeOnly.FromDateTime(this.UtcNow);
        var sociableTimeStartUtc = TimeOnly.Parse(this.opts.Value.SociableTimeStartUtc);
        var sociableTimeEndUtc = TimeOnly.Parse(this.opts.Value.SociableTimeEndUtc);

        return utcTimeNow < sociableTimeStartUtc
               || utcTimeNow >= sociableTimeEndUtc;
    }
}
