namespace CovidLetter.Backend.Common.Infrastructure.HealthCheck;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Diagnostics.HealthChecks;
public class HealthCheck
{
    private readonly IHealthCheckService healthCheckService;

    public HealthCheck(IHealthCheckService healthCheckService)
    {
        this.healthCheckService = healthCheckService;
    }

    internal IDictionary<HealthStatus, int> ResultStatusCodes { get; } = new Dictionary<HealthStatus, int>(new Dictionary<HealthStatus, int>
    {
        { HealthStatus.Healthy, StatusCodes.Status200OK },
        { HealthStatus.Degraded, StatusCodes.Status200OK },
        { HealthStatus.Unhealthy, StatusCodes.Status503ServiceUnavailable },
    });

    public async Task<IActionResult> InvokeAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var result = await this.healthCheckService.CheckHealthAsync(cancellationToken);
        if (this.ResultStatusCodes.TryGetValue(result.Status, out var statusCode))
        {
            return new ObjectResult(result.Status.ToString())
            {
                StatusCode = statusCode,
                ContentTypes = new MediaTypeCollection { "text/plain" },
            };
        }

        throw new InvalidOperationException(
            $"No status code mapping found for {nameof(HealthStatus)} value: {result.Status}");
    }
}
