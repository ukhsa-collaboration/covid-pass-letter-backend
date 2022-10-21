namespace CovidLetter.Backend.Common.Application.Logger;

using Microsoft.Extensions.Logging;

public static class AppEventId
{
    public static EventId SuccessfulInboundRequestReceived { get; } =
        new(2500, nameof(SuccessfulInboundRequestReceived));

    public static EventId SuccessfulRequestReceived { get; } = new(2510, nameof(SuccessfulRequestReceived));

    public static EventId SuccessfulLetterGeneration { get; } = new(2520, nameof(SuccessfulLetterGeneration));

    public static EventId SuccessfulNotificationSent { get; } = new(2521, nameof(SuccessfulNotificationSent));

    public static EventId FailedLetterRequestHistory { get; } = new(4500, nameof(FailedLetterRequestHistory));

    public static EventId FailedLetterGeneration { get; } = new(4510, nameof(FailedLetterGeneration));

    public static EventId FailedRecoveryLetterGeneration { get; } = new(4511, nameof(FailedRecoveryLetterGeneration));

    public static EventId FailedNotificationSent { get; } = new(4521, nameof(FailedNotificationSent));

    public static EventId ErrorLetterGeneration { get; } = new(5500, nameof(ErrorLetterGeneration));

    public static EventId ErrorSendingSftp { get; } = new(5501, nameof(ErrorSendingSftp));

    public static EventId ErrorCopyingFiles { get; } = new(5502, nameof(ErrorCopyingFiles));

    public static EventId ErrorParsingFile { get; } = new(5503, nameof(ErrorParsingFile));

    public static EventId ErrorEnrichingLetter { get; } = new(5504, nameof(ErrorEnrichingLetter));

    public static EventId ErrorCleaningUpPreviousBatches { get; } = new(5505, nameof(ErrorCleaningUpPreviousBatches));

    public static EventId ErrorCreatingBatch { get; } = new(5506, nameof(ErrorCreatingBatch));

    public static EventId ErrorConvertingJsonToCSV { get; } = new(5507, nameof(ErrorConvertingJsonToCSV));

    public static EventId FailedAddToQueue { get; } = new(5508, nameof(FailedAddToQueue));

    public static EventId ErrorPersistingLetterDuplicatePrimaryKey { get; } = new(5509, nameof(ErrorPersistingLetterDuplicatePrimaryKey));

    public static EventId ErrorReadingBankHolidayEmbeddedResource { get; } = new(5510, nameof(ErrorReadingBankHolidayEmbeddedResource));
}
