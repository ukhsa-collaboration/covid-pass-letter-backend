namespace CovidLetter.Backend.Common.Application.Notifications;

using CovidLetter.Backend.Common.Options;

public interface INotificationService
{
    Task SendEmailAsync(
        string address,
        string templateId,
        NotificationPersonalisation? personalisation = null,
        string? correlationId = null);

    Task SendLetterAsync(
        NotificationLetterAddress letterAddress,
        string templateId,
        NotificationPersonalisation? personalisation = null,
        string? correlationId = null);

    Task SendSmsAsync(
        string number,
        string templateId,
        NotificationPersonalisation? personalisation = null,
        string? correlationId = null);

    string? GetTemplateIdOrDefault(Func<NotificationTemplateConfiguration, bool> predicate, ContactMethodType method);
}
