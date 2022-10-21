namespace CovidLetter.Backend.Processing.Functions;

using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Application.Notifications;
using CovidLetter.Backend.Common.Infrastructure.Postgres;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class SendFailureNotificationFunction
{
    private readonly ILogger<SendFailureNotificationFunction> log;
    private readonly INotificationService notificationService;
    private readonly AppFunction appFunction;
    private readonly PostgresLetterStore postgresLetterStore;
    private readonly IClock clock;
    private readonly IGuidGenerator guidGenerator;

    public SendFailureNotificationFunction(
        ILogger<SendFailureNotificationFunction> log,
        INotificationService notificationService,
        AppFunction appFunction,
        PostgresLetterStore postgresLetterStore,
        IClock clock,
        IGuidGenerator guidGenerator)
    {
        this.log = log;
        this.notificationService = notificationService;
        this.appFunction = appFunction;
        this.postgresLetterStore = postgresLetterStore;
        this.clock = clock;
        this.guidGenerator = guidGenerator;
    }

    [FunctionName(AppFunction.FailureNotification)]
    public async Task Run(
        [ServiceBusTrigger(
            $"%{nameof(FunctionOptions.FailureTopic)}%",
            $"%{nameof(FunctionOptions.FailureNotificationV1Subscription)}%",
            Connection = "ServiceBus")]
        string messageBody,
        string correlationId,
        string messageId,
        IDictionary<string, object> userProperties)
    {
        await this.appFunction.RunAsync(AppFunction.FailureNotification, async () =>
        {
            await this.RunAsync(new QueuedMessage(messageId, messageBody, correlationId, userProperties));
        });
    }

    private async Task RunAsync(QueuedMessage message)
    {
        var failureNotification = message.DeserializeAndValidate<FailureNotification>(MessageVersions.V1);
        var letterRequest = failureNotification.Request;

        switch (letterRequest.ContactMethod)
        {
            case ContactMethodType.DoNotContact:
                this.log.LogInformation(
                    "Letter request {CorrelationId} does not include notification channel preference",
                    letterRequest.CorrelationId);
                return;
            case ContactMethodType.Letter:
                await this.PersistNotificationLetter(failureNotification, message.Id);
                break;
            case ContactMethodType.Email:
            case ContactMethodType.SMS:
            default:
                await this.NotifyUsingGovNotify(failureNotification);
                break;
        }
    }

    private async Task NotifyUsingGovNotify(FailureNotification failureNotification)
    {
        var personalisation = failureNotification.ToCommonNotificationPersonalisation();
        var templateId = this.GetTemplateIdByMethodAndCode(failureNotification.Request.ContactMethod, failureNotification.ReasonCode);
        if (string.IsNullOrWhiteSpace(templateId))
        {
            this.log.LogInformation(
                "Letter request {CorrelationId} will not be notified due to missing template for {ReasonCode}",
                failureNotification.Request.CorrelationId,
                failureNotification.ReasonCode);
            return;
        }

        switch (failureNotification.Request)
        {
            case { ContactMethod: ContactMethodType.Email }:
                this.log.LogInformation(
                    "Letter request {CorrelationId} will be notified by email using template {TemplateId}",
                    failureNotification.Request.CorrelationId,
                    templateId);
                await this.notificationService.SendEmailAsync(
                    failureNotification.Request.EmailAddress,
                    templateId,
                    personalisation,
                    failureNotification.Request.CorrelationId);
                break;
            case { ContactMethod: ContactMethodType.SMS }:
                this.log.LogInformation(
                    "Letter request {CorrelationId} will be notified by text message using template {TemplateId}",
                    failureNotification.Request.CorrelationId,
                    templateId);
                await this.notificationService.SendSmsAsync(
                    failureNotification.Request.MobileNumber,
                    templateId,
                    personalisation,
                    failureNotification.Request.CorrelationId);
                break;
        }
    }

    private string GetTemplateIdByMethodAndCode(ContactMethodType contactMethodType, NotificationReasonCode reasonCode)
    {
        // non-public reason codes do not have notification templates associated with them
        if (reasonCode < 0)
        {
            return string.Empty;
        }

        var templateId = this.notificationService.GetTemplateIdOrDefault(t => t.Code == reasonCode, contactMethodType);
        if (!string.IsNullOrWhiteSpace(templateId))
        {
            return templateId;
        }

        this.log.LogWarning(
            "Unable to find template ID for {ContactMethodType} for code {ReasonCode} in configuration",
            contactMethodType,
            reasonCode);

        return string.Empty;
    }

    private async Task PersistNotificationLetter(
        FailureNotification failureNotification,
        string messageId)
    {
        this.log.LogInformation(
            "Letter request {CorrelationId} will be notified by letter",
            failureNotification.Request.CorrelationId);

        var letter = CanonicalLetter.FromLetterRequest(
            new CanonicalLetterRequest(
                failureNotification.Request,
                this.guidGenerator.NewGuid(),
                messageId,
                this.clock.UtcNow,
                true));
        var wrapper = letter.ToFailureLetterWrapper(this.clock, failureNotification.ReasonCode);

        await this.postgresLetterStore.AddAsync(wrapper);
    }
}
