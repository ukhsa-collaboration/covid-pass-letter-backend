namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

using CovidLetter.Backend.Common.Application.Constants;
using Microsoft.Extensions.Configuration;

public class ConfigurationPostgresConnectionStringProvider
    : IPostgresConnectionStringProvider
{
    private readonly IConfiguration configuration;

    public ConfigurationPostgresConnectionStringProvider(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public string ConnectionString => this.configuration.GetConnectionString(ConnectionStrings.Postgres);
}
