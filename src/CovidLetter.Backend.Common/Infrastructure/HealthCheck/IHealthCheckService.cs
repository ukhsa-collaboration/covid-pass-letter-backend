namespace CovidLetter.Backend.Common.Infrastructure.HealthCheck;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public interface IHealthCheckService
{
    /// <summary>
    /// Runs all the health checks in the application and returns the aggregated status.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the health checks.</param>
    /// <returns>
    /// A <see cref="Task{T}"/> which will complete when all the health checks have been run,
    /// yielding a <see cref="HealthReport"/> containing the results.
    /// </returns>
    public Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return this.CheckHealthAsync(predicate: null, cancellationToken);
    }

    /// <summary>
    /// Runs the provided health checks and returns the aggregated status
    /// </summary>
    /// <param name="predicate">
    /// A predicate that can be used to include health checks based on user-defined criteria.
    /// </param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the health checks.</param>
    /// <returns>
    /// A <see cref="Task{T}"/> which will complete when all the health checks have been run,
    /// yielding a <see cref="HealthReport"/> containing the results.
    /// </returns>
    public Task<HealthReport> CheckHealthAsync(
        Func<HealthCheckRegistration, bool>? predicate,
        CancellationToken cancellationToken = default);
}
