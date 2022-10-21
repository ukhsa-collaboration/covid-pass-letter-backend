namespace CovidLetter.Backend.Common.Application.Notifications;

public static class NotificationExtensions
{
    /// <summary>
    /// Builds all address components into a new object in order to be able to notify
    /// a citizen via the `letter` channel.
    /// </summary>
    /// <param name="letterRequest">The letter request to extract values from.</param>
    /// <returns>All postal address components.</returns>
    public static NotificationLetterAddress ToNotificationAddress(this LetterRequest letterRequest)
    {
        return new NotificationLetterAddress(
            letterRequest.FullName,
            letterRequest.AddressLine1,
            letterRequest.AddressLine2,
            letterRequest.AddressLine3,
            letterRequest.AddressLine4,
            letterRequest.Postcode);
    }

    /// <summary>
    /// Convert an address of potentially empty fields into a dictionary containing only
    /// non-empty values with keys incrementing in the format `address_line_n` as per the
    /// spec at: https://docs.notifications.service.gov.uk/net.html#send-a-letter.
    /// </summary>
    /// <param name="address">The address to convert.</param>
    /// <returns>The dictionary containing `address_line_n` values.</returns>
    public static NotificationPersonalisation ToNotificationPersonalisation(this NotificationLetterAddress address)
    {
        var addressComponents = new[]
        {
            address.AddressLine1,
            address.AddressLine2,
            address.AddressLine3,
            address.AddressLine4,
            address.AddressLine5,
            address.AddressLine6,
            address.AddressLine7,
        };

        var activeAddressComponents = addressComponents.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (activeAddressComponents.Count < 3)
        {
            throw new ArgumentException("At least three address components are required", nameof(address));
        }

        var personalisation = new NotificationPersonalisation();
        var i = 0;
        foreach (var addressComponent in activeAddressComponents)
        {
            i++;
            personalisation.Add($"address_line_{i}", addressComponent);
        }

        return personalisation;
    }

    /// <summary>
    /// Build a personalisation dictionary containing items that are common to all notification
    /// templates.
    /// </summary>
    /// <param name="failureNotification">The failure notification to extract values from.</param>
    /// <returns>The common dictionary elements.</returns>
    public static NotificationPersonalisation ToCommonNotificationPersonalisation(this FailureNotification failureNotification) =>
        new()
        {
            ["first_name"] = failureNotification.Request.FirstName,
            ["last_name"] = failureNotification.Request.LastName,
            ["error_code"] = (int)failureNotification.ReasonCode,
        };
}
