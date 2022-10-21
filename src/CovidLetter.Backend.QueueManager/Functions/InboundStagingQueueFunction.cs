namespace CovidLetter.Backend.QueueManager.Functions;

using CovidLetter.Backend.QueueManager.Application;
using CovidLetter.Backend.QueueManager.Configuration;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

public class InboundStagingQueueFunction
{
    private const string FunctionName = "InboundStagingQueue";
    private readonly ServiceBusHelper serviceBusHelper;
    private readonly Func<ExtendedFunctionOptions, string> inboundStagingQueueSelector = t => t.InboundStagingQueue;

    public InboundStagingQueueFunction(ServiceBusHelper serviceBusHelper)
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
                this.inboundStagingQueueSelector,
                cancellationToken));
}
