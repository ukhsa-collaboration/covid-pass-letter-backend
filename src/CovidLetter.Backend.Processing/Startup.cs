namespace CovidLetter.Backend.Processing;

using System.Reflection;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Application.Profiles;
using CovidLetter.Backend.Common.Infrastructure;
using CovidLetter.Backend.Common.Infrastructure.AzureFiles;
using CovidLetter.Backend.Common.Options;
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
                c.AddFileServiceClient(configuration.GetConnectionString(ConnectionStrings.OutputStorageAccount))
                    .WithName(AzureFileSystem.OutputStorage);
                c.AddServiceBusClient(configuration.GetConnectionString(ConnectionStrings.ServiceBus));
            });

        builder.Services
            .AddConfiguration(configuration)
            .AddAppInsightsTelemetry(configuration)
            .AddStandardServices()
            .AddSingleton<AzureFileSystem>()
            .AddFileSerializerServices()
            .AddSingleton<FeatureToggle>()
            .AddBarcodeServices()
            .AddCertificateServices()
            .AddNotificationServices(configuration)
            .AddSingleton<IQueuePoster, QueuePoster>()
            .AddDataStore()
            .AddHealthCheckServices();

        builder.Services
            .AddCustomHttpClients()
            .AddMemoryCache();

        builder.Services.AddAutoMapper(typeof(LetterProfile));
    }
}
