namespace CovidLetter.Backend.Common.Infrastructure;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Certificates;
using CovidLetter.Backend.Common.Application.Certificates.Domestic;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Infrastructure.Barcodes;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Hl7.Fhir.Model;
using Hl7.Fhir.Model.R4;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

public class CertificateClient : ICertificateClient
{
    private readonly IClock clock;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<CertificateClient> log;
    private readonly BarcodeOptions options;
    private readonly FeatureToggle featureToggle;

    public CertificateClient(
        IHttpClientFactory httpClientFactory,
        IOptions<BarcodeOptions> options,
        ILogger<CertificateClient> log,
        IClock clock,
        FeatureToggle featureToggle)
    {
        this.httpClientFactory = httpClientFactory;
        this.options = options.Value;
        this.log = log;
        this.clock = clock;
        this.featureToggle = featureToggle;
    }

    public Task<GetCertificateResponse<QrCodeResponse<IntlVaccinationResponse>>> GetUnattendedVaccinationCertificateAsync(
        Patient patient,
        FileCountry country,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return TaskHelpers.WithDelay(
            TimeSpan.FromSeconds(1),
            async () => await this.GetQrCodeResponse<IntlVaccinationResponse>(
                patient,
                country,
                this.options.UnattendedCertificateVaccinationEndpointPath,
                "vaccination",
                correlationId,
                cancellationToken));
    }

    public Task<GetCertificateResponse<DomesticCertificateResponse>> GetUnattendedDomesticCertificateAsync(
        Patient patient,
        FileCountry country,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return TaskHelpers.WithDelay<GetCertificateResponse<DomesticCertificateResponse>>(
            TimeSpan.FromSeconds(1),
            async () =>
            {
                using var httpClient = this.GetHttpClientForCountry(country);

                this.log.LogInformation(
                    "Getting domestic certificate for {Id} at {TimeStamp} from {Url} with subscription key {Key}",
                    correlationId,
                    this.clock.UtcNow.ToString("R"),
                    $"{httpClient.BaseAddress}{this.options.UnattendedCertificateDomesticEndpointPath}",
                    ParseKeyFromRequestHeaders(httpClient.DefaultRequestHeaders));

                using var response = await httpClient.PostAsync(
                    this.options.UnattendedCertificateDomesticEndpointPath,
                    new StringContent(patient.ToJson(Version.R4), Encoding.UTF8, MediaTypeNames.Application.Json),
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var certificate = await response.Content.ReadFromJsonAsync<DomesticCertificateResponse>(
                    JsonConfig.Default,
                    cancellationToken);

                if (certificate == null)
                {
                    return ErrorResponseCode.UnknownError;
                }

                if (certificate.ErrorCode != null)
                {
                    return certificate.ErrorCode.Value;
                }

                return certificate;
            });
    }

    public Task<GetCertificateResponse<QrCodeResponse<IntlRecoveryResponse>>> GetUnattendedRecoveryCertificateAsync(
        Patient patient,
        FileCountry country,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (this.featureToggle.IsEnabled(FeatureToggle.UnattendedRecoveryApiDisabled))
        {
            return Task.FromResult(
                new GetCertificateResponse<QrCodeResponse<IntlRecoveryResponse>>(ErrorResponseCode
                    .NoTestResultsGrantingRecoveryFound));
        }

        return TaskHelpers.WithDelay(
            TimeSpan.FromSeconds(1),
            async () => await this.GetQrCodeResponse<IntlRecoveryResponse>(
                patient,
                country,
                this.options.UnattendedCertificateRecoveryEndpointPath,
                "recovery",
                correlationId,
                cancellationToken));
    }

    private async Task<GetCertificateResponse<QrCodeResponse<T>>> GetQrCodeResponse<T>(
        Base patient,
        FileCountry country,
        string endpointPath,
        string endpointName,
        string correlationId,
        CancellationToken cancellationToken)
        where T : IGenericBarcodeResponse
    {
        using var httpClient = this.GetHttpClientForCountry(country);

        this.log.LogInformation(
            "Getting unattended {EndpointName} certificate for {Id} at {TimeStamp} from {Url} with subscription key {Key}",
            endpointName,
            correlationId,
            this.clock.UtcNow.ToString("R"),
            $"{httpClient.BaseAddress}{endpointPath}",
            ParseKeyFromRequestHeaders(httpClient.DefaultRequestHeaders));

        using var response = await httpClient.PostAsync(
            endpointPath,
            new StringContent(patient.ToJson(Version.R4), Encoding.UTF8, MediaTypeNames.Application.Json),
            cancellationToken);

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var errorResponse = await JsonSerializer.DeserializeAsync<ErrorResponse>(
            responseStream,
            JsonConfig.Default,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            responseStream.Position = 0;

            var qrCodeResponse = await JsonSerializer.DeserializeAsync<QrCodeResponse<T>>(
                responseStream,
                JsonConfig.Default,
                cancellationToken);

            if (qrCodeResponse?.ResultData != null)
            {
                return qrCodeResponse;
            }
        }
        else
        {
            if (errorResponse is { ErrorCode: > ErrorResponseCode.UnknownError })
            {
                this.log.LogWarning(
                    "Unsuccessful status code {StatusCode} returned from unattended {EndpointName} API. API Error {ApiError}",
                    (int)response.StatusCode,
                    endpointName,
                    (int)errorResponse.ErrorCode);
            }

            response.EnsureSuccessStatusCode();
        }

        return errorResponse?.ErrorCode ?? ErrorResponseCode.UnknownError;
    }

    private static string GetClientNameFromCountry(FileCountry country) =>
        country switch
        {
            FileCountry.England => StringConsts.CertificateClientGb,
            FileCountry.IsleOfMan => StringConsts.CertificateClientIm,
            FileCountry.Wales => StringConsts.CertificateClientWales,
            _ => throw new NotSupportedException("Unknown country code: " + country),
        };

    private HttpClient GetHttpClientForCountry(FileCountry country) =>
        this.httpClientFactory.CreateClient(GetClientNameFromCountry(country));

    private static string ObfuscateSubscriptionKey(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? "<missing>"
            : $"{key[..4]}****{key[^4..]}";

    private static string ParseKeyFromRequestHeaders(HttpHeaders headers) =>
        headers.TryGetValues(StringConsts.OcpApimSubscriptionKeyHeader, out var keys)
            ? ObfuscateSubscriptionKey(keys.SingleOrDefault())
            : "<missing>";
}
