namespace CovidLetter.Backend.Common.Application;

using CovidLetter.Backend.Common.Application.Barcodes;
using CovidLetter.Backend.Common.Application.Barcodes.Validators;
using CovidLetter.Backend.Common.Application.Certificates;
using CovidLetter.Backend.Common.Application.Logger;
using CovidLetter.Backend.Common.Infrastructure.Barcodes;
using CovidLetter.Backend.Common.Options;
using Hl7.Fhir.Model.R4;
using Microsoft.Extensions.Options;
using NodaTime;
using IClock = CovidLetter.Backend.Common.Utilities.IClock;

public interface ICertificateProvider
{
    Task<CanonicalLetter> MakeLetterAsync(string messageId, LetterRequest letterRequest);
}

public class CertificateProvider : ICertificateProvider
{
    private readonly BarcodeConverter barcodeConverter;
    private readonly IBarcodePublicKeyProvider publicKeyProvider;
    private readonly BarcodeOptions barcodeOptions;
    private readonly ICertificateClient certificateClient;
    private readonly AppEventLogger<CertificateProvider> appEventLogger;
    private readonly IClock clock;

    public CertificateProvider(
        BarcodeConverter barcodeConverter,
        IBarcodePublicKeyProvider publicKeyProvider,
        IOptions<BarcodeOptions> barcodeOptions,
        ICertificateClient certificateClient,
        AppEventLogger<CertificateProvider> appEventLogger,
        IClock clock)
    {
        this.barcodeConverter = barcodeConverter;
        this.publicKeyProvider = publicKeyProvider;
        this.barcodeOptions = barcodeOptions.Value;
        this.certificateClient = certificateClient;
        this.appEventLogger = appEventLogger;
        this.clock = clock;
    }

    public async Task<CanonicalLetter> MakeLetterAsync(string messageId, LetterRequest letterRequest)
    {
        if (!letterRequest.IsVaccineLetter())
        {
            throw new ArgumentException("Letter request must include a vaccine letter");
        }

        var patient = FhirMapper.MapRequest(letterRequest);
        var country = CanonicalLetterRequest.RegionToCountry(letterRequest.Region);

        var letterTypes = new List<string>();
        var publicKeys = await this.publicKeyProvider.GetKeysAsync();

        var travelPasses = new List<VaccineTravelPass>();
        var travelPassesResult =
            await this.GetAndValidateTravelPassesAsync(messageId, patient, country, publicKeys);
        if (travelPassesResult.IsSuccessful)
        {
            travelPasses = travelPassesResult.Response;
            letterTypes.Add(LetterRequest.VaccineLetter);
        }
        else
        {
            this.appEventLogger.LogLetterInformationEvent(
                AppEventId.FailedLetterGeneration,
                letterRequest,
                "Unable to produce travel pass due to {ApiError}",
                (int)travelPassesResult.ErrorCode);
        }

        var recoveryPasses = new List<RecoveryPass>();
        if (patient.Is5To11YearsOld(this.clock))
        {
            var recoveryPassesResult =
                await this.GetAndValidateRecoveryPassesAsync(messageId, patient, country, publicKeys);

            if (recoveryPassesResult.IsSuccessful)
            {
                recoveryPasses = recoveryPassesResult.Response;
                letterTypes.Add(LetterRequest.Recovery);
            }
            else
            {
                this.appEventLogger.LogLetterInformationEvent(
                    AppEventId.FailedRecoveryLetterGeneration,
                    letterRequest,
                    "Unable to produce recovery pass due to {ApiError}",
                    (int)recoveryPassesResult.ErrorCode);
            }
        }

        if (!travelPasses.Any() && !recoveryPasses.Any())
        {
            throw new FailedToRetrieveCertificateException(travelPassesResult.ErrorCode);
        }

        return CanonicalLetter.FromLetterRequest(
            new CanonicalLetterRequest(
                letterRequest,
                Guid.TryParse(messageId, out var id) ? id : Guid.NewGuid(),
                messageId,
                DateTime.UtcNow,
                letterTypes,
                travelPasses,
                recoveryPasses));
    }

    private async Task<GetCertificateResponse<List<VaccineTravelPass>>> GetAndValidateTravelPassesAsync(
        string messageId,
        Patient patient,
        FileCountry country,
        IReadOnlyDictionary<string, string> publicKeys)
    {
        var qrCodeResponse =
            await this.certificateClient.GetUnattendedVaccinationCertificateAsync(patient, country, messageId);

        return qrCodeResponse.MapIfSuccessful(response =>
        {
            var validator = BarcodeValidatorFactory.MakeInternationalCertificate(publicKeys, this.barcodeOptions);
            var barcodes = response.ResultData.Select(d => d.QRCode)
                .ToDictionary(c => c, this.barcodeConverter.ConvertToBarcode<InternationalSchema>);

            validator.Validate(barcodes.Values, patient);

            VaccineTravelPass MakeTravelPass(IntlVaccinationResponse vaccine)
            {
                var expiry = Instant.FromUnixTimeSeconds(barcodes[vaccine.QRCode].Certificate.Exp).ToDateTimeUtc();
                return new VaccineTravelPass
                {
                    VaccinationEvent = new VaccinationEvent
                    {
                        VaccineFriendlyName = vaccine.DisplayName,
                        VaccineManufacturer =
                            new CodedValue(vaccine.VaccineManufacturer.Item1, vaccine.VaccineManufacturer.Item2),
                        LocationDose = vaccine.Site,
                        DateOfDose = vaccine.DateTimeOfTest,
                        VaccineBatchNumber = vaccine.VaccineBatchNumber,
                        VaccineOrProphylaxis = new CodedValue(vaccine.VaccineType.Item1, vaccine.VaccineType.Item2),
                        VaccineBrand = new CodedValue(vaccine.Product.Item1, vaccine.Product.Item2),
                        DoseNumber = vaccine.DoseNumber,
                        CountryOfVaccination = vaccine.CountryOfVaccination,
                        CertificateIssuer = vaccine.Authority,
                        VaccineNumerator = vaccine.DoseNumber,
                        VaccineDenominator = vaccine.TotalSeriesOfDoses,
                        VaccineProductCode = vaccine.SnomedCode,
                    },
                    Uvci = response.UniqueCertificateIdentifier,
                    QrCode = vaccine.QRCode,
                    QrCodeExpiry = expiry,
                };
            }

            return response.ResultData.Select(MakeTravelPass).ToList();
        });
    }

    private async Task<GetCertificateResponse<List<RecoveryPass>>> GetAndValidateRecoveryPassesAsync(
        string messageId,
        Patient patient,
        FileCountry country,
        IReadOnlyDictionary<string, string> publicKeys)
    {
        var qrCodeResponse =
            await this.certificateClient.GetUnattendedRecoveryCertificateAsync(patient, country, messageId);

        return qrCodeResponse.MapIfSuccessful(response =>
        {
            var validator = BarcodeValidatorFactory.MakeRecoveryCertificate(publicKeys, this.barcodeOptions);
            var barcodes = response.ResultData.Select(d => d.QRCode)
                .ToDictionary(c => c, this.barcodeConverter.ConvertToBarcode<RecoverySchema>);

            validator.Validate(barcodes.Values, patient);

            RecoveryPass MakeRecoveryPass(IntlRecoveryResponse recovery)
            {
                var expiry = Instant.FromUnixTimeSeconds(barcodes[recovery.QRCode].Certificate.Exp).ToDateTimeUtc();
                return new RecoveryPass
                {
                    DateOfPositiveTestResult = recovery.DateTimeOfTest,
                    TypeOfTest = recovery.TestType!,
                    DiseaseTargeted = new CodedValue(recovery.DiseaseTargeted.Item1, recovery.DiseaseTargeted.Item2),
                    CountryOfTest = recovery.CountryOfAuthority!,

                    DateValidFrom = this.clock.UtcNow.Date,

                    DateValidTo = expiry,
                    RecoveryUvci = response.UniqueCertificateIdentifier,
                    RecoveryBarcode = recovery.QRCode,
                    RecoveryBarcodeEndDate = expiry,
                };
            }

            return response.ResultData.Select(MakeRecoveryPass).ToList();
        });
    }
}
