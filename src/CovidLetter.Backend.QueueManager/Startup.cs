// ReSharper disable UnusedMethodReturnValue.Local
namespace CovidLetter.Backend.QueueManager;

using System.Reflection;
using Azure.Core.Extensions;
using Azure.Messaging.ServiceBus;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Infrastructure;
using CovidLetter.Backend.QueueManager.Application;
using CovidLetter.Backend.QueueManager.Configuration;
using CovidLetter.Backend.QueueManager.Profiles;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        var context = builder.GetContext();

        builder.ConfigurationBuilder
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true);
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;

        builder.Services
            .AddAzureClients(c =>
            {
                c.AddServiceBusClient(configuration.GetConnectionString(ConnectionStrings.ServiceBusSender));
                AddServiceBusClientReceiver(c, configuration.GetConnectionString(ConnectionStrings.ServiceBusReceiver));
                AddServiceBusClientSender(c, configuration.GetConnectionString(ConnectionStrings.ServiceBusSender));
                c.AddServiceAdministrationBusClient(configuration.GetConnectionString(ConnectionStrings.ServiceBusReceiver));
            });

        builder.Services
            .AddConfiguration(configuration)
            .Configure<QueueOptions>(configuration)
            .Configure<FailureNotificationOptions>(configuration)
            .Configure<ExtendedFunctionOptions>(configuration)
            .AddAppInsightsTelemetry(configuration)
            .AddSingleton<IQueuePoster, QueuePoster>()
            .AddSingleton<ServiceBusHelper>()
            .AddSingleton<FailureNotificationHelper>()
            .AddAutoMapper(typeof(MessageProfile))
            .AddHealthCheckServices();
    }

    private static IAzureClientBuilder<ServiceBusClientReceiver, ServiceBusClientOptions>
        AddServiceBusClientReceiver<TBuilder>(TBuilder builder, string connectionString)
        where TBuilder : IAzureClientFactoryBuilder
    {
        return builder.RegisterClientFactory<ServiceBusClientReceiver, ServiceBusClientOptions>(options =>
            new ServiceBusClientReceiver(connectionString, options));
    }

    private static IAzureClientBuilder<ServiceBusClientSender, ServiceBusClientOptions>
        AddServiceBusClientSender<TBuilder>(TBuilder builder, string connectionString)
        where TBuilder : IAzureClientFactoryBuilder
    {
        return builder.RegisterClientFactory<ServiceBusClientSender, ServiceBusClientOptions>(options =>
            new ServiceBusClientSender(connectionString, options));
    }
}
