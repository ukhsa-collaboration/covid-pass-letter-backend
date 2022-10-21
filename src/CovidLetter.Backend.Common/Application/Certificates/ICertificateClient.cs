namespace CovidLetter.Backend.Common.Application.Certificates;

using CovidLetter.Backend.Common.Application;
using CovidLetter.Backend.Common.Application.Certificates.Domestic;
using Hl7.Fhir.Model.R4;

public interface ICertificateClient
{
    /// <summary>
    ///     Calls remote API used to produce international / travel certificate based on vaccination data.
    /// </summary>
    /// <param name="patient">
    ///     FHIR <see cref="Patient" /> containing <see cref="Patient.Identifier" />,
    ///     <see cref="Patient.Name" /> and <see cref="Patient.BirthDate" />.
    /// </param>
    /// <param name="country">A <see cref="FileCountry" /> enum determining which endpoint to use.</param>
    /// <param name="correlationId">Request correlation ID for logging</param>
    /// <param name="cancellationToken">A cancellation token (optional)</param>
    /// <returns>A fully formed <see cref="QrCodeResponse{T}" /> from the remote API.</returns>
    /// <exception cref="FailedToRetrieveCertificateException">
    ///     Thrown if failed to retrieve a <see cref="QrCodeResponse{T}" />
    ///     from the remote API. See <see cref="FailedToRetrieveCertificateException.ErrorCode" /> for details.
    /// </exception>
    Task<GetCertificateResponse<QrCodeResponse<IntlVaccinationResponse>>> GetUnattendedVaccinationCertificateAsync(
        Patient patient,
        FileCountry country,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<GetCertificateResponse<DomesticCertificateResponse>> GetUnattendedDomesticCertificateAsync(
        Patient patient,
        FileCountry country,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<GetCertificateResponse<QrCodeResponse<IntlRecoveryResponse>>> GetUnattendedRecoveryCertificateAsync(
        Patient patient,
        FileCountry country,
        string correlationId,
        CancellationToken cancellationToken = default);
}
