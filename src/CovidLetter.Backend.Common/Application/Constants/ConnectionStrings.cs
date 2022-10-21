namespace CovidLetter.Backend.Common.Application.Constants;

public static class ConnectionStrings
{
    public static readonly string InputStorageAccount = nameof(InputStorageAccount);

    public static readonly string OutputStorageAccount = nameof(OutputStorageAccount);

    public static readonly string ServiceBus = nameof(ServiceBus);

    public static readonly string CSBConnectionString = nameof(CSBConnectionString);

    public static readonly string Postgres = "postgres";

    public static readonly string PostgresAdmin = "postgres_admin";
}
