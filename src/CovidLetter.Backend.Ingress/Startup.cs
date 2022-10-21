namespace CovidLetter.Backend.Ingress;

using System.Reflection;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Infrastructure;
using CovidLetter.Backend.Common.Infrastructure.AzureFiles;
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

        builder.Services
            .AddAzureClients(c =>
            {
                c.AddFileServiceClient(configuration.GetConnectionString(ConnectionStrings.InputStorageAccount))
                    .WithName(AzureFileSystem.InputStorage);
                c.AddFileServiceClient(configuration.GetConnectionString(ConnectionStrings.OutputStorageAccount))
                    .WithName(AzureFileSystem.OutputStorage);
                c.AddServiceBusClient(configuration.GetConnectionString(ConnectionStrings.ServiceBus));
            });

        builder.Services
            .AddConfiguration(configuration)
            .AddStandardServices()
            .AddSingleton<AzureFileSystem>()
            .AddSingleton<IQueuePoster, QueuePoster>()
            .AddFileSerializerServices()
            .AddAppInsightsTelemetry(configuration)
            .AddHealthCheckServices();
    }
}
