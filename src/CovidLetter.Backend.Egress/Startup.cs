namespace CovidLetter.Backend.Egress;

using System.Reflection;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Infrastructure;
using CovidLetter.Backend.Common.Infrastructure.AzureFiles;
using CovidLetter.Backend.Common.Infrastructure.SftpFiles;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        builder.ConfigurationBuilder.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;

        if (configuration == null)
        {
            throw new Exception("Configuration missing");
        }

        builder.Services.AddAzureClients(c =>
        {
            c.AddFileServiceClient(configuration.GetConnectionString(ConnectionStrings.OutputStorageAccount))
                .WithName(AzureFileSystem.OutputStorage);
        });

        builder.Services
            .AddConfiguration(configuration)
            .AddAppInsightsTelemetry(configuration)
            .AddStandardServices()
            .AddSingleton<AzureFileSystem>()
            .AddSingleton<SftpFileSystem>()
            .AddHealthCheckServices();
    }
}
