namespace CovidLetter.Backend.QueueManager.Application;

using System.ComponentModel.DataAnnotations;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Notifications;

public sealed class EnqueueFailureNotificationRequest
{
    internal const string GovNotifySmokeTestEmailAddress = "simulate-delivered@notifications.service.gov.uk";
    internal const string GovNotifySmokeTestPhoneNumber = "07700900000";

    [Required]
    [Range((int)ContactMethodType.Email, (int)ContactMethodType.SMS, ErrorMessage = "The {0} field must equal Email or SMS.")]
    public ContactMethodType? ContactMethod { get; init; }

    [Required]
    public NotificationReasonCode? ReasonCode { get; init; }

    [Required]
    public string? FirstName { get; init; }

    [Required]
    public string? LastName { get; init; }

    public string? Recipient { get; init; }

    internal FailureNotification ToFailureNotification(string? correlationId = null) =>
        new()
        {
            ReasonCode = this.ReasonCode!.Value,
            Request = new LetterRequest
            {
                ContactMethod = this.ContactMethod!.Value,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
                EmailAddress = this.ContactMethod == ContactMethodType.Email
                    ? this.Recipient ?? GovNotifySmokeTestEmailAddress
                    : string.Empty,
                FirstName = this.FirstName!,
                LastName = this.LastName!,
                MobileNumber = this.ContactMethod == ContactMethodType.SMS
                    ? this.Recipient ?? GovNotifySmokeTestPhoneNumber
                    : string.Empty,
            },
        };
}
