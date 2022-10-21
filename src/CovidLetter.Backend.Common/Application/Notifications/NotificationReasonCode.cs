namespace CovidLetter.Backend.Common.Application.Notifications;

/// <summary>
/// Reason codes as defined at https://www.nhs.uk/contact-us/covid-status-letter-service-help/.
/// </summary>
public enum NotificationReasonCode
{
    /// <summary>
    /// No immunization or recovery certificate available.
    /// </summary>
    MissingVaccinationRecord = 3006,

    /// <summary>
    /// Vaccination record doesn't show the correct dosage number, timing, or manufacturer requirements.
    /// </summary>
    IncompleteVaccinationRecord = 3012,
}
