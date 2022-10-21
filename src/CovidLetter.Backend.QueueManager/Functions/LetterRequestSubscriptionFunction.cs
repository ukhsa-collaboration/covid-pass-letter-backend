namespace CovidLetter.Backend.QueueManager.Functions;

using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.QueueManager.Application;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

public class LetterRequestSubscriptionFunction
{
    private const string FunctionName = "LetterRequestSubscription";
    private readonly ServiceBusHelper serviceBusHelper;
    private readonly Func<ServiceBusEnqueueOptions, string> successTopicSelector = t => t.SuccessTopic;

    private readonly Func<FunctionOptions, string> letterRequestSubscriptionSelector =
        t => t.LetterRequestV1Subscription;

    public LetterRequestSubscriptionFunction(ServiceBusHelper serviceBusHelper)
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
                this.successTopicSelector,
                this.letterRequestSubscriptionSelector,
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
                this.successTopicSelector,
                this.letterRequestSubscriptionSelector,
                cancellationToken));
}
