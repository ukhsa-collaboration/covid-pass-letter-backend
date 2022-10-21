namespace CovidLetter.Backend.Processing.Functions;

using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Application.Logger;
using CovidLetter.Backend.Common.Infrastructure.Postgres;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NodaTime;
using IClock = CovidLetter.Backend.Common.Utilities.IClock;

public class LetterRequestHistoryFunction
{
    private readonly AppFunction appFunction;
    private readonly PostgresLetterRequestStore letterRequestStore;
    private readonly IClock clock;
    private readonly ILogger<LetterRequestHistoryFunction> log;
    private readonly IQueuePoster queuePoster;
    private readonly AppEventLogger<LetterRequestHistoryFunction> appEventLogger;

    public LetterRequestHistoryFunction(
        ILogger<LetterRequestHistoryFunction> log,
        AppFunction appFunction,
        IQueuePoster queuePoster,
        PostgresLetterRequestStore letterRequestStore,
        IClock clock,
        AppEventLogger<LetterRequestHistoryFunction> appEventLogger)
    {
        this.log = log;
        this.appFunction = appFunction;
        this.queuePoster = queuePoster;
        this.letterRequestStore = letterRequestStore;
        this.clock = clock;
        this.appEventLogger = appEventLogger;
    }

    [FunctionName(AppFunction.LetterRequestHistory)]
    public Task Run(
        [ServiceBusTrigger(
            $"%{nameof(FunctionOptions.InboundTopic)}%",
            $"%{nameof(FunctionOptions.RollingLetterRequestV1Subscription)}%",
            Connection = "ServiceBus")]
        string messageBody,
        string correlationId,
        string messageId,
        IDictionary<string, object> userProperties) =>
        this.appFunction.RunAsync(
            AppFunction.LetterRequestHistory,
            async () => await this.RunAsync(new QueuedMessage(
                messageId,
                messageBody,
                correlationId,
                userProperties)));

    private async Task RunAsync(QueuedMessage message)
    {
        this.log.LogInformation("Message {MessageId} for {CorrelationId} received", message.Id, message.CorrelationId);

        var letterRequest = message.DeserializeAndValidate<LetterRequest>(MessageVersions.V1);
        this.appEventLogger.LogSuccessfulInboundRequestReceived(letterRequest, "Successful - {MessageId}", message.Id);

        var dateOfThisRequest = this.clock.GetCurrentInstant();
        var sevenDaysAgo = dateOfThisRequest.Minus(Duration.FromDays(7));

        var success = await this.letterRequestStore
            .AddRequestIfNoneInExclusionPeriodAsync(
                letterRequest,
                sevenDaysAgo,
                dateOfThisRequest);

        if (success)
        {
            this.log.LogInformation("Letter request {CorrelationId} accepted; forwarding to success topic.", letterRequest.CorrelationId);

            await this.SendSuccessMessageAsync(letterRequest);
        }
        else
        {
            this.appEventLogger.LogLetterInformationEvent(
                AppEventId.FailedLetterRequestHistory,
                letterRequest,
                "Rejected - existing request in last seven days.");
        }
    }

    private async Task SendSuccessMessageAsync(LetterRequest letterRequest)
    {
        var message = this.queuePoster.MakeJsonMessage(
            letterRequest.CorrelationId,
            letterRequest,
            MessageVersions.V1);
        await this.queuePoster.SendMessageAsync(message, o => o.SuccessTopic);
    }
}
