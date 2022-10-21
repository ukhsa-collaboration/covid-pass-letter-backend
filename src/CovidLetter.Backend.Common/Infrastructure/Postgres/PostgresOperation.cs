namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

using CovidLetter.Backend.Common.Infrastructure.Telemetry;
using Dapper;
using Microsoft.ApplicationInsights;
using Npgsql;

public class PostgresOperation
{
    private readonly string name;
    private readonly NpgsqlConnection connection;
    private readonly NpgsqlTransaction? transaction;
    private readonly TelemetryClient telemetryClient;

    public PostgresOperation(string name, NpgsqlConnection connection, NpgsqlTransaction? transaction, TelemetryClient telemetryClient)
    {
        this.name = name;
        this.connection = connection;
        this.transaction = transaction;
        this.telemetryClient = telemetryClient;
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using (this.telemetryClient.TrackPostgresOperation(this.connection, this.name, sql))
        {
            return await this.connection.ExecuteAsync(sql, param, this.transaction);
        }
    }

    public async Task<T> ExecuteScalarAsync<T>(string sql, object? param = null)
    {
        using (this.telemetryClient.TrackPostgresOperation(this.connection, this.name, sql))
        {
            return await this.connection.ExecuteScalarAsync<T>(sql, param, this.transaction);
        }
    }

    public async Task<T> QuerySingleOrDefaultAsync<T>(string sql, object? param = null)
    {
        using (this.telemetryClient.TrackPostgresOperation(this.connection, this.name, sql))
        {
            return await this.connection.QuerySingleOrDefaultAsync<T>(sql, param, this.transaction);
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        using (this.telemetryClient.TrackPostgresOperation(this.connection, this.name, sql))
        {
            return await this.connection.QueryAsync<T>(sql, param, this.transaction);
        }
    }
}
