namespace CovidLetter.Backend.QueueManager.Application;

using System.Text.Json;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class FailureNotificationHelper
{
    private static readonly Func<ServiceBusEnqueueOptions, string> FailureTopicFunc = opts => opts.FailureTopic;
    private readonly FunctionOptions functionOptions;
    private readonly ILogger<FailureNotificationHelper> logger;
    private readonly IQueuePoster queuePoster;

    public FailureNotificationHelper(
        IOptions<FunctionOptions> functionOptions,
        IQueuePoster queuePoster,
        ILogger<FailureNotificationHelper> logger)
    {
        this.functionOptions = functionOptions.Value;
        this.queuePoster = queuePoster;
        this.logger = logger;
    }

    public Task SendFailureNotificationMessageAsync(EnqueueFailureNotificationRequest request, string? correlationId = null)
    {
        var failureNotification = request.ToFailureNotification(correlationId);

        var message = this.queuePoster.MakeJsonMessage(failureNotification.Request.CorrelationId, failureNotification, MessageVersions.V1);

        this.LogSendingMessageToTopic(failureNotification.Request.CorrelationId);
        return this.queuePoster.SendMessageAsync(message, FailureTopicFunc);
    }

    private void LogSendingMessageToTopic(string correlationId) =>
        this.logger.LogInformation(
            "Sending failure notification message to topic \"{messageQueueTopic}\" with correlationId=\"{correlationId}\".",
            FailureTopicFunc(this.functionOptions),
            correlationId);
}
