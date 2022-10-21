namespace CovidLetter.Backend.QueueManager.Functions;

using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.QueueManager.Application;
using CovidLetter.Backend.QueueManager.Configuration;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

public class SubscriptionSummaryFunction
{
    private const string FunctionName = "SubscriptionSummary";
    private readonly ServiceBusHelper serviceBusHelper;

    private readonly Func<ExtendedFunctionOptions, string> inboundStagingQueueSelector = t => t.InboundStagingQueue;
    private readonly Func<ServiceBusEnqueueOptions, string> inboundTopicSelector = t => t.InboundTopic;
    private readonly Func<ServiceBusEnqueueOptions, string> failureTopicSelector = t => t.FailureTopic;
    private readonly Func<ServiceBusEnqueueOptions, string> successTopicSelector = t => t.SuccessTopic;

    private readonly Func<FunctionOptions, string> letterRequestSubscriptionSelector =
        t => t.LetterRequestV1Subscription;

    private readonly Func<FunctionOptions, string> failureNotificationSubscriptionSelector =
        t => t.FailureNotificationV1Subscription;

    private readonly Func<FunctionOptions, string> letterRequestHistorySubscriptionSelector =
        t => t.RollingLetterRequestV1Subscription;

    public SubscriptionSummaryFunction(ServiceBusHelper serviceBusHelper)
    {
        this.serviceBusHelper = serviceBusHelper;
    }

    [UsedImplicitly]
    [FunctionName(FunctionName)]
    public async Task<IActionResult> GetMessagesAsync(
        [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Get), Route = FunctionName)]
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        var messageRequest = new ServiceBusEntityRequest
        {
            TopicSelector = this.inboundTopicSelector,
            SubscriptionSelector = this.letterRequestHistorySubscriptionSelector,
            CancellationToken = cancellationToken,
        };

        var letterRequestHistorySubscription = await this.serviceBusHelper.GetSubscriptionMessageCountAsync(messageRequest);

        messageRequest.TopicSelector = this.successTopicSelector;
        messageRequest.SubscriptionSelector = this.letterRequestSubscriptionSelector;
        var letterRequestSubscription = await this.serviceBusHelper.GetSubscriptionMessageCountAsync(messageRequest);

        messageRequest.TopicSelector = this.failureTopicSelector;
        messageRequest.SubscriptionSelector = this.failureNotificationSubscriptionSelector;
        var failureNotificationSubscription = await this.serviceBusHelper.GetSubscriptionMessageCountAsync(messageRequest);

        messageRequest.QueueSelector = this.inboundStagingQueueSelector;
        var inboundStagingQueue = await this.serviceBusHelper.GetQueueMessageCountAsync(messageRequest);

        return new OkObjectResult(new
        {
            inboundStagingQueue,
            letterRequestHistorySubscription,
            letterRequestSubscription,
            failureNotificationSubscription,
        });
    }
}
