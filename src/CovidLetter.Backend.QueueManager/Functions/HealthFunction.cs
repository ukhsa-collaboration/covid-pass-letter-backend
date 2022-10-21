namespace CovidLetter.Backend.QueueManager.Functions;

using CovidLetter.Backend.Common.Infrastructure;
using CovidLetter.Backend.Common.Infrastructure.HealthCheck;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

public class HealthFunction
{
    private const string FunctionName = "Health";
    private readonly HealthCheck healthCheck;

    public HealthFunction(HealthCheck healthCheck)
    {
        this.healthCheck = healthCheck;
    }

    [FunctionName(FunctionName)]
    public async Task<IActionResult> HealthAsync(
        [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Get), Route = FunctionName)]
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        return await this.healthCheck.InvokeAsync(request, cancellationToken);
    }
}
