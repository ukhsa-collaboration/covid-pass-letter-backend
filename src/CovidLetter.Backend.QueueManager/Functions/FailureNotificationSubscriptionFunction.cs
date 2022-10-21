namespace CovidLetter.Backend.QueueManager.Functions;

using System.Text.Json;
using System.Web.Http;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using CovidLetter.Backend.QueueManager.Application;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Options;

public class FailureNotificationSubscriptionFunction
{
    private const string FunctionName = "FailureNotificationSubscription";
    private readonly FailureNotificationHelper failureNotificationHelper;
    private readonly FailureNotificationOptions failureNotificationOptions;

    private readonly Func<FunctionOptions, string> failureNotificationSubscriptionSelector =
        t => t.FailureNotificationV1Subscription;

    private readonly Func<ServiceBusEnqueueOptions, string> failureTopicSelector = t => t.FailureTopic;
    private readonly ServiceBusHelper serviceBusHelper;

    public FailureNotificationSubscriptionFunction(
        ServiceBusHelper serviceBusHelper,
        FailureNotificationHelper failureNotificationHelper,
        IOptions<FailureNotificationOptions> opts)
    {
        this.serviceBusHelper = serviceBusHelper;
        this.failureNotificationHelper = failureNotificationHelper;
        this.failureNotificationOptions = opts.Value;
    }

    [UsedImplicitly]
    [FunctionName(FunctionName + EntityNameFormatter.EnqueueFailureNotificationSuffix)]
    public async Task<IActionResult> EnqueueFailureNotificationAsync(
        [HttpTrigger(
            AuthorizationLevel.Function,
            nameof(HttpMethods.Post),
            Route = $"{FunctionName}{EntityNameFormatter.PathDelimiter}{EntityNameFormatter.EnqueueFailureNotificationSuffix}")]
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = GetCorrelationId(request) ?? Guid.NewGuid().ToString();

        EnqueueFailureNotificationRequest? failureNotificationRequest;
        try
        {
            failureNotificationRequest = await JsonSerializer.DeserializeAsync<EnqueueFailureNotificationRequest>(
                request.Body,
                JsonConfig.Default,
                cancellationToken);

            ValidationHelpers.Validate(failureNotificationRequest!);

            if (!string.IsNullOrWhiteSpace(failureNotificationRequest!.Recipient)
                && !this.RecipientIsOnWhitelist(failureNotificationRequest!.Recipient))
            {
                return new BadRequestErrorMessageResult($"The {nameof(failureNotificationRequest.Recipient)} field is restricted to whitelisted values.");
            }
        }
        catch (JsonException)
        {
            return new BadRequestErrorMessageResult("Invalid JSON.");
        }
        catch (ArgumentException ex)
        {
            return new BadRequestErrorMessageResult(ex.Message);
        }

        await this.failureNotificationHelper.SendFailureNotificationMessageAsync(failureNotificationRequest!, correlationId);

        return new OkObjectResult(new { correlationId });
    }

    [UsedImplicitly]
    [FunctionName(FunctionName)]
    public async Task<IActionResult> GetMessagesAsync(
        [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Get), Route = FunctionName)]
        HttpRequest request,
        CancellationToken cancellationToken = default) =>
        new OkObjectResult(
            await this.serviceBusHelper.GetMessagesAsync(
                this.failureTopicSelector,
                this.failureNotificationSubscriptionSelector,
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
                this.failureTopicSelector,
                this.failureNotificationSubscriptionSelector,
                cancellationToken));

    private static string? GetCorrelationId(HttpRequest request) =>
        request.HttpContext.Features
            .Get<RequestTelemetry>()
            ?.Context.Operation.Id;

    private bool RecipientIsOnWhitelist(string recipient) =>
        this.failureNotificationOptions.RecipientWhitelist.Contains(recipient);
}
