namespace CovidLetter.Backend.Common.Infrastructure.HealthCheck;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public class HealthCheckServiceWrapper : IHealthCheckService
{
    private readonly HealthCheckService healthCheckService;

    public HealthCheckServiceWrapper(HealthCheckService healthCheckService)
    {
        this.healthCheckService = healthCheckService;
    }

    public Task<HealthReport> CheckHealthAsync(
        Func<HealthCheckRegistration, bool>? predicate,
        CancellationToken cancellationToken = default)
    {
        return this.healthCheckService.CheckHealthAsync(predicate, cancellationToken);
    }
}
