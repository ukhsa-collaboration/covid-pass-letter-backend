namespace CovidLetter.Backend.Common.Options;

using System.ComponentModel.DataAnnotations;

public class ClockOptions
{
    /// <summary>
    ///     Sociable Hours Start Time
    /// </summary>
    /// <remarks>Format: <c>hh:mm</c></remarks>
    [RegularExpression(@"^([0-1][0-9]|2[0-3]):([0-5][0-9])$", ErrorMessage = "Format must match 'hh:mm'")]
    public string SociableTimeStartUtc { get; set; } = "08:00";

    /// <summary>
    ///     Sociable Hours End Time
    /// </summary>
    /// <remarks>Format: <c>hh:mm</c></remarks>
    [RegularExpression(@"^([0-1][0-9]|2[0-3]):([0-5][0-9])$", ErrorMessage = "Format must match 'hh:mm'")]
    public string SociableTimeEndUtc { get; set; } = "20:00";
}
