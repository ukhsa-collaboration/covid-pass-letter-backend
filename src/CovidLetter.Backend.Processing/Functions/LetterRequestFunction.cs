namespace CovidLetter.Backend.Processing.Functions;

using Azure.Messaging.ServiceBus;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Certificates;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Application.Logger;
using CovidLetter.Backend.Common.Application.Notifications;
using CovidLetter.Backend.Common.Infrastructure.Postgres;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class LetterRequestFunction
{
    private readonly AppFunction appFunction;
    private readonly PostgresLetterStore letterStore;
    private readonly ILogger<LetterRequestFunction> log;
    private readonly IQueuePoster queuePoster;
    private readonly ICertificateProvider certificateProvider;
    private readonly AppEventLogger<LetterRequestFunction> appEventLogger;
    private readonly IClock clock;

    public LetterRequestFunction(
        ILogger<LetterRequestFunction> log,
        AppFunction appFunction,
        IQueuePoster queuePoster,
        PostgresLetterStore letterStore,
        ICertificateProvider certificateProvider,
        AppEventLogger<LetterRequestFunction> appEventLogger,
        IClock clock)
    {
        this.log = log;
        this.appFunction = appFunction;
        this.queuePoster = queuePoster;
        this.letterStore = letterStore;
        this.certificateProvider = certificateProvider;
        this.appEventLogger = appEventLogger;
        this.clock = clock;
    }

    [FunctionName(AppFunction.LetterRequest)]
    public Task Run(
        [ServiceBusTrigger(
            $"%{nameof(FunctionOptions.SuccessTopic)}%",
            $"%{nameof(FunctionOptions.LetterRequestV1Subscription)}%",
            Connection = "ServiceBus")]
        string messageBody,
        string correlationId,
        string messageId,
        IDictionary<string, object> userProperties) =>
        this.appFunction.RunAsync(
            AppFunction.LetterRequest,
            async () =>
            {
                await this.RunAsync(new QueuedMessage(
                    messageId,
                    messageBody,
                    correlationId,
                    userProperties));
            });

    internal static NotificationReasonCode? MapCertificateErrorCodeToNotificationReason(ErrorResponseCode errorResponseCode) =>
        errorResponseCode switch
        {
            ErrorResponseCode.PatientIsTooYoungToObtainRequestedPass => NotificationReasonCode.IncompleteVaccinationRecord,
            ErrorResponseCode.NoVaccineRecordsFound => NotificationReasonCode.IncompleteVaccinationRecord,
            ErrorResponseCode.NoCertificateGeneratedDueToVaccinationHistoryNotMeetingCriteria => NotificationReasonCode.MissingVaccinationRecord,
            ErrorResponseCode.NoTestResultsGrantingRecoveryFound => NotificationReasonCode.MissingVaccinationRecord,
            _ => null,
        };

    private async Task RunAsync(QueuedMessage message)
    {
        this.log.LogInformation("Message {MessageId} for {CorrelationId} received", message.Id, message.CorrelationId);

        if (await this.letterStore.AlreadyAddedAsync(message.Id))
        {
            this.log.LogWarning(
                AppEventId.ErrorPersistingLetterDuplicatePrimaryKey,
                "Message {MessageId} for {CorrelationId} has already been enriched and persisted. No further message processing will be done.",
                message.Id,
                message.CorrelationId);

            return;
        }

        var letterRequest = message.DeserializeAndValidate<LetterRequest>(MessageVersions.V1);

        this.appEventLogger.LogLetterInformationEvent(
            AppEventId.SuccessfulRequestReceived,
            letterRequest,
            "Successful - {MessageId}",
            message.Id);

        try
        {
            var certificate = await this.certificateProvider.MakeLetterAsync(message.Id, letterRequest);

            await this.letterStore.AddAsync(certificate.ToLetterWrapper(this.clock));

            this.appEventLogger.LogLetterInformationEvent(
                AppEventId.SuccessfulLetterGeneration,
                letterRequest,
                $"Successful - {{{nameof(LetterRequest.LetterType)}}}",
                string.Join(", ", certificate.LetterType));
        }
        catch (FailedToRetrieveCertificateException ex)
            when (ex.ErrorCode != ErrorResponseCode.UnknownError)
        {
            var notificationReasonCode = MapCertificateErrorCodeToNotificationReason(ex.ErrorCode);

            this.appEventLogger.LogLetterErrorEvent(
                AppEventId.FailedLetterGeneration,
                ex,
                letterRequest,
                "Failed - API Error {ApiError}, Platform Error {PlatformError}",
                (int)ex.ErrorCode,
                notificationReasonCode.HasValue ? (int)notificationReasonCode : "[unknown]");

            if (notificationReasonCode.HasValue)
            {
                await this.SendFailureMessageAsync(letterRequest, notificationReasonCode.Value);
            }
        }
        catch (Exception ex)
        {
            this.appEventLogger.LogLetterErrorEvent(
                AppEventId.ErrorLetterGeneration,
                ex,
                letterRequest,
                "Failed - {Error}",
                ex.Message);

            throw;
        }
    }

    private async Task SendFailureMessageAsync(LetterRequest letterRequest, NotificationReasonCode reasonCode)
    {
        ServiceBusMessage message;

        if (letterRequest.ContactMethod.Equals(ContactMethodType.SMS) && this.clock.IsOutsideSociableHours())
        {
            message = this.queuePoster.MakeJsonMessage(
                letterRequest.CorrelationId,
                new FailureNotification { ReasonCode = reasonCode, Request = letterRequest },
                MessageVersions.V1,
                scheduledEnqueueTime: this.clock.GetNextTimeInsideSociableHours());
        }
        else
        {
            message = this.queuePoster.MakeJsonMessage(
                letterRequest.CorrelationId,
                new FailureNotification { ReasonCode = reasonCode, Request = letterRequest },
                MessageVersions.V1);
        }

        await this.queuePoster.SendMessageAsync(message, o => o.FailureTopic);
    }
}
