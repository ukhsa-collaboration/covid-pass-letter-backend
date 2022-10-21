namespace CovidLetter.Backend.Common.Infrastructure;

using System.ComponentModel;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.BankHolidays;
using CovidLetter.Backend.Common.Application.Barcodes;
using CovidLetter.Backend.Common.Application.Certificates;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Application.Logger;
using CovidLetter.Backend.Common.Application.Notifications;
using CovidLetter.Backend.Common.Application.Serialization;
using CovidLetter.Backend.Common.Infrastructure.Barcodes;
using CovidLetter.Backend.Common.Infrastructure.HealthCheck;
using CovidLetter.Backend.Common.Infrastructure.Postgres;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

public static class ServiceCollectionExtensions
{
    private const string BulkheadPolicyKey = nameof(BulkheadPolicyKey);

    public static IServiceCollection AddCustomHttpClients(this IServiceCollection services)
    {
        services.AddPolicyRegistry().Add(BulkheadPolicyKey, GetBulkheadPolicy(services));

        AddUnattendedCertificateApiHttpClient(
            services,
            StringConsts.CertificateClientGb,
            o => o.UnattendedCertificateApiKeyHeaderValueGb);

        AddUnattendedCertificateApiHttpClient(
            services,
            StringConsts.CertificateClientIm,
            o => o.UnattendedCertificateApiKeyHeaderValueIm);

        AddUnattendedCertificateApiHttpClient(
            services,
            StringConsts.CertificateClientWales,
            o => o.UnattendedCertificateApiKeyHeaderValueWales);

        services.AddHttpClient(StringConsts.BarcodePublicKeyClient, (sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<BarcodeOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.DevolvedAdministrationPublicKeysBaseUrl);
        });

        return services;
    }

    public static IServiceCollection AddAppInsightsTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        var telemetry = TelemetryConfiguration.CreateDefault();
        telemetry.InstrumentationKey = configuration[StringConsts.AppInsightsKey] ?? string.Empty;

        services.AddSingleton(typeof(AppEventLogger<>));
        return services;
    }

    /// <summary>
    /// Adds services that should really be globally available. Please make sure they are truly generic
    /// such as replacements for system static methods. These will likely be included in all runtimes, so
    /// keep it lightweight.
    /// </summary>
    public static IServiceCollection AddStandardServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IChaosMonkey, NullChaosMonkey>()
            .AddSingleton<IGuidGenerator, SystemGuidGenerator>()
            .AddSingleton<IClock, SystemClock>()
            .AddSingleton<IBankHolidayService, ResourceFileBankHolidayService>();
    }

    public static IServiceCollection AddBarcodeServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IBarcodeDecoder, BarcodeDecoder>()
            .AddSingleton<IBarcodePublicKeyProvider, BarcodePublicKeyProvider>();
    }

    public static IServiceCollection AddCertificateServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<BarcodeConverter>()
            .AddSingleton<ICertificateClient, CertificateClient>()
            .AddSingleton<ICertificateProvider, CertificateProvider>();
    }

    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddTransient<INotificationService, NotificationService>()
            .Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));
    }

    public static IServiceCollection AddDataStore(this IServiceCollection services)
    {
        PostgresRunner.Configure();

        return services
            .AddSingleton<PostgresRunner>()
            .AddSingleton<IPostgresConnectionStringProvider, ConfigurationPostgresConnectionStringProvider>()
            .AddSingleton<PostgresLetterStore>()
            .AddSingleton<PostgresLetterRequestStore>();
    }

    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ClockOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations();

        return services
            .Configure<BarcodeOptions>(configuration)
            .Configure<FunctionOptions>(configuration)
            .Configure<StorageOptions>(configuration)
            .Configure<ResourceFileBankHolidayServiceOptions>(configuration)
            .AddSingleton<AppFunction>()
            .AddSingleton<FeatureToggle>();
    }

    public static IServiceCollection AddFileSerializerServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IFileSerializerFactory, FileSerializerFactory>()
            .AddSingleton(typeof(FileSerializer<>))
            .AddSingleton<IFileGeneratorFactory<Letter>, LetterFileGeneratorFactory>()
            .AddSingleton<IFileGeneratorFactory<FailureLetter>, FailureLetterFileGeneratorFactory>();
    }

    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<HealthCheck.HealthCheck>()
            .AddSingleton<IHealthCheckService, HealthCheckServiceWrapper>()
            .AddHealthChecks()
            .Services;
    }

    private static void AddUnattendedCertificateApiHttpClient(
        IServiceCollection services,
        string httpClientName,
        Func<BarcodeOptions, string> apiKeyPredicate)
    {
        services
            .AddHttpClient(httpClientName, (sp, httpClient) =>
            {
                var options = sp.GetRequiredService<IOptions<BarcodeOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.UnattendedCertificateApiBaseUrl);
                if (!string.IsNullOrWhiteSpace(options.UnattendedCertificateApiKeyHeaderName))
                {
                    httpClient.DefaultRequestHeaders.Add(
                        options.UnattendedCertificateApiKeyHeaderName,
                        apiKeyPredicate(options));
                }
            })
            .AddPolicyHandlerFromRegistry(BulkheadPolicyKey);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetBulkheadPolicy(IServiceCollection services)
    {
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<BarcodeOptions>>().Value;
        var bulkhead = Policy.BulkheadAsync(options.UnattendedCertificateApiMaxParallelization, 100);
        return bulkhead.AsAsyncPolicy<HttpResponseMessage>();
    }
}
