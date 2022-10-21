namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

public interface IPostgresConnectionStringProvider
{
    string ConnectionString { get; }
}
