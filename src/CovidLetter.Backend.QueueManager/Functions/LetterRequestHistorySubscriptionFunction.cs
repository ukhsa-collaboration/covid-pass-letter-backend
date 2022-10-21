namespace CovidLetter.Backend.QueueManager.Functions;

using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.QueueManager.Application;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

public class LetterRequestHistorySubscriptionFunction
{
    private const string FunctionName = "LetterRequestHistorySubscription";
    private readonly ServiceBusHelper serviceBusHelper;
    private readonly Func<ServiceBusEnqueueOptions, string> inboundTopicSelector = t => t.InboundTopic;

    private readonly Func<FunctionOptions, string> letterRequestHistorySubscriptionSelector =
        t => t.RollingLetterRequestV1Subscription;

    public LetterRequestHistorySubscriptionFunction(ServiceBusHelper serviceBusHelper)
    {
        this.serviceBusHelper = serviceBusHelper;
    }

    [UsedImplicitly]
    [FunctionName(FunctionName)]
    public async Task<IActionResult> GetMessagesAsync(
        [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Get), Route = FunctionName)]
        HttpRequest request,
        CancellationToken cancellationToken = default) =>
        new OkObjectResult(
            await this.serviceBusHelper.GetMessagesAsync(
                this.inboundTopicSelector,
                this.letterRequestHistorySubscriptionSelector,
                cancellationToken));

    [UsedImplicitly]
    [FunctionName(FunctionName + EntityNameFormatter.DeadLetterQueueSuffix)]
    public async Task<IActionResult> ReprocessDeadLettersAsync(
        [HttpTrigger(
            AuthorizationLevel.Function,
            nameof(HttpMethods.Post),
            Route = $"{FunctionName}{EntityNameFormatter.PathDelimiter}{EntityNameFormatter.DeadLetterQueueSuffix}")]
        HttpRequest request,
        CancellationToken cancellationToken = default) =>
        new OkObjectResult(
            await this.serviceBusHelper.ReprocessDeadLettersAsync(
                this.inboundTopicSelector,
                this.letterRequestHistorySubscriptionSelector,
                cancellationToken));
}
