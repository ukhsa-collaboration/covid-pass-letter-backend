// ReSharper disable MemberCanBePrivate.Global
namespace CovidLetter.Backend.Common.Options;

public class FunctionOptions : ServiceBusEnqueueOptions
{
    public string LetterRequestV1Subscription { get; set; } = string.Empty;

    public string RollingLetterRequestV1Subscription { get; set; } = string.Empty;

    public string FailureNotificationV1Subscription { get; set; } = string.Empty;

    public string? EnabledFunctions { get; set; }

    public string? EnabledFeatures { get; set; }

    /// <summary>
    /// Gets or sets the number of letters in a single file (Our printers have a limit of half a mill rows).
    /// </summary>
    public int MaxLettersPerFile { get; set; } = 500_000;

    public bool UseFakeBarcodes { get; set; }
}
