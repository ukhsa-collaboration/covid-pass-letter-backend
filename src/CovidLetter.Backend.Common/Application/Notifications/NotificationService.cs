namespace CovidLetter.Backend.Common.Application.Notifications;

using CovidLetter.Backend.Common.Application.Logger;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notify.Client;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> log;
    private readonly NotificationOptions options;
    private readonly IClock clock;
    private readonly IHttpClientFactory httpClientFactory;

    public NotificationService(
        IOptions<NotificationOptions> options,
        ILogger<NotificationService> log,
        IClock clock,
        IHttpClientFactory httpClientFactory)
    {
        this.log = log;
        this.options = options.Value;
        this.clock = clock;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task SendEmailAsync(string address, string templateId, NotificationPersonalisation? personalisation = null, string? correlationId = null)
    {
        Ensure.NotNullOrWhiteSpace(address);
        Ensure.NotNullOrWhiteSpace(templateId);

        try
        {
            var response = await this.WithClient(async client =>
                await client.SendEmailAsync(address, templateId, personalisation, correlationId));

            this.log.LogInformation(
                AppEventId.SuccessfulNotificationSent,
                "Email with ID {Id}, CorrelationId {CorrelationId}, Template {Template} and Uri {Uri} was sent at {SentAt}",
                response.id,
                response.reference,
                response.template.id,
                response.uri,
                this.clock.UtcNow);
        }
        catch (Exception e)
        {
            this.log.LogError(
                AppEventId.FailedNotificationSent,
                e,
                "Email failed to send with TemplateId {TemplateId} and CorrelationId {CorrelationId}",
                templateId,
                correlationId);
            throw new Exception(
                $"Email failed to send with TemplateId {templateId} and CorrelationId {correlationId}",
                e);
        }
    }

    public async Task SendLetterAsync(NotificationLetterAddress address, string templateId, NotificationPersonalisation? personalisation = null, string? correlationId = null)
    {
        Ensure.NotNull(address);
        Ensure.NotNullOrWhiteSpace(templateId);

        // add mandatory address components to personalisation
        personalisation ??= new NotificationPersonalisation();
        personalisation.AddRange(address.ToNotificationPersonalisation());

        try
        {
            var response = await this.WithClient(async client =>
                await client.SendLetterAsync(templateId, personalisation, correlationId));

            this.log.LogInformation(
                AppEventId.SuccessfulNotificationSent,
                "Letter with ID {Id}, CorrelationId {CorrelationId}, Template {Template} and Uri {Uri} was sent at {SentAt}",
                response.id,
                response.reference,
                response.template.id,
                response.uri,
                this.clock.UtcNow);
        }
        catch (Exception e)
        {
            this.log.LogError(
                AppEventId.FailedNotificationSent,
                e,
                "Letter failed to send with TemplateId {TemplateId} and CorrelationId {CorrelationId}",
                templateId,
                correlationId);
            throw new Exception(
                $"Letter failed to send with TemplateId {templateId} and CorrelationId {correlationId}",
                e);
        }
    }

    public async Task SendSmsAsync(string number, string templateId, NotificationPersonalisation? personalisation = null, string? correlationId = null)
    {
        Ensure.NotNullOrWhiteSpace(number);
        Ensure.NotNullOrWhiteSpace(templateId);

        try
        {
            var response = await this.WithClient(async client =>
                await client.SendSmsAsync(number, templateId, personalisation, correlationId));

            this.log.LogInformation(
                AppEventId.SuccessfulNotificationSent,
                "SMS with ID {Id}, CorrelationId {CorrelationId}, Template {Template} and Uri {Uri} was sent at {SentAt}",
                response.id,
                response.reference,
                response.template.id,
                response.uri,
                this.clock.UtcNow);
        }
        catch (Exception e)
        {
            this.log.LogError(
                AppEventId.FailedNotificationSent,
                e,
                "SMS failed to send with TemplateId {TemplateId} and CorrelationId {CorrelationId}",
                templateId,
                correlationId);
            throw new Exception(
                $"SMS failed to send with TemplateId {templateId} and CorrelationId {correlationId}",
                e);
        }
    }

    public string? GetTemplateIdOrDefault(Func<NotificationTemplateConfiguration, bool> predicate, ContactMethodType method)
    {
        Ensure.NotNull(this.options.Configuration);

        return this.options.Configuration
             .Where(predicate)
             .SelectMany(c => c.Templates.Where(t => t.Key == method))
             .Select(t => t.Value)
             .FirstOrDefault();
    }

    private async Task<T> WithClient<T>(Func<NotificationClient, Task<T>> action)
    {
        using var httpClient = this.httpClientFactory.CreateClient();
        using var wrapper = new HttpClientWrapper(httpClient);
        return await action(new NotificationClient(wrapper, this.options.ApiKey));
    }
}
