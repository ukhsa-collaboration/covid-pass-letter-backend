namespace CovidLetter.Backend.Common.Application.Notifications;

// Gov.Notify spec for sending letters requires at least three address components
public record NotificationLetterAddress(
    string AddressLine1,
    string AddressLine2,
    string AddressLine3,
    string AddressLine4 = default!,
    string AddressLine5 = default!,
    string AddressLine6 = default!,
    string AddressLine7 = default!);
