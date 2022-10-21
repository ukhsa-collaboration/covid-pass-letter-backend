namespace CovidLetter.Backend.Common.Infrastructure.Telemetry;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Npgsql;

public static class TelemetryClientExtensions
{
    public static IOperationHolder<DependencyTelemetry> TrackPostgresOperation(
        this TelemetryClient telemetryClient,
        NpgsqlConnection connection,
        string operationName,
        string sql = null!)
    {
        var operation = telemetryClient.StartOperation<DependencyTelemetry>(operationName);
        operation.Telemetry.Target = connection.Host;
        operation.Telemetry.Type = "Database";
        operation.Telemetry.Data = sql;
        return operation;
    }
}
