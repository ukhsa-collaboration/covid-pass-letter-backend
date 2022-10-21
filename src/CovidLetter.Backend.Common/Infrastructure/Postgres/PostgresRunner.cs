namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

using System.Data;
using Dapper;
using Microsoft.ApplicationInsights;
using NodaTime;
using Npgsql;
using Polly;

public class PostgresRunner
{
    private static readonly IAsyncPolicy PostgresPolicy
        = Policy.Handle<PostgresException>(ex => ex.IsTransient)
            .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i));

    private readonly IPostgresConnectionStringProvider connectionStringProvider;
    private readonly TelemetryClient telemetryClient;

    public PostgresRunner(IPostgresConnectionStringProvider connectionStringProvider, TelemetryClient telemetryClient)
    {
        this.connectionStringProvider = connectionStringProvider;
        this.telemetryClient = telemetryClient;
    }

    public static void Configure()
    {
        NpgsqlConnection.GlobalTypeMapper.UseNodaTime();
        SqlMapper.AddTypeMap(typeof(Instant), DbType.Object);
        SqlMapper.AddTypeHandler(new JsonFieldHandler());
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public async Task RunAsync(string operationName, Func<PostgresOperation, Task> act)
    {
        await PostgresPolicy.ExecuteAsync(async () =>
        {
            await using var database = new NpgsqlConnection(this.connectionStringProvider.ConnectionString);
            await database.OpenAsync();
            await act(new PostgresOperation(operationName, database, null, this.telemetryClient));
        });
    }

    public async Task<T> RunAsync<T>(string operationName, Func<PostgresOperation, Task<T>> act)
    {
        return await PostgresPolicy.ExecuteAsync(async () =>
        {
            await using var database = new NpgsqlConnection(this.connectionStringProvider.ConnectionString);
            await database.OpenAsync();
            return await act(new PostgresOperation(operationName, database, null, this.telemetryClient));
        });
    }

    public async Task RunSerializableAsync(string operationName, Func<PostgresOperation, Task> act)
    {
        await PostgresPolicy.ExecuteAsync(async () =>
        {
            await using var database = new NpgsqlConnection(this.connectionStringProvider.ConnectionString);
            await database.OpenAsync();
            await using var transaction = await database.BeginTransactionAsync(IsolationLevel.Serializable);
            await act(new PostgresOperation(operationName, database, transaction, this.telemetryClient));
            await transaction.CommitAsync(); // We will only automatically commit if no exception is thrown
        });
    }

    public async Task<T> RunSerializableAsync<T>(string operationName, Func<PostgresOperation, Task<T>> act)
    {
        return await PostgresPolicy.ExecuteAsync(async () =>
        {
            await using var database = new NpgsqlConnection(this.connectionStringProvider.ConnectionString);
            await database.OpenAsync();
            await using var transaction = await database.BeginTransactionAsync(IsolationLevel.Serializable);
            var result = await act(new PostgresOperation(operationName, database, transaction, this.telemetryClient));
            await transaction.CommitAsync(); // We will only automatically commit if no exception is thrown
            return result;
        });
    }
}
