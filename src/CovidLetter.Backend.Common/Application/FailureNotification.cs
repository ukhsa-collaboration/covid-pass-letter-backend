namespace CovidLetter.Backend.Common.Application;

using System.ComponentModel.DataAnnotations;
using CovidLetter.Backend.Common.Application.Notifications;

public class FailureNotification
{
    public NotificationReasonCode ReasonCode { get; init; }

    [Required]
    public LetterRequest Request { get; init; } = null!;

    public static string GetReasonText(NotificationReasonCode reasonCode) =>
        reasonCode switch
        {
            NotificationReasonCode.IncompleteVaccinationRecord =>
                "Vaccination record doesn't show the correct dosage number, timing, or manufacturer requirements",
            NotificationReasonCode.MissingVaccinationRecord => "No record found",
            _ => throw new ArgumentOutOfRangeException(nameof(reasonCode)),
        };
}
