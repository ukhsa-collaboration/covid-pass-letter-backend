namespace CovidLetter.Backend.Common.Application.Certificates.Domestic;

public enum CertificateType
{
    Diagnostic,
    Vaccination,
    Immunity,
    Recovery,
    Exemption,
    None,
    DomesticVoluntary,
    DomesticMandatory,

    // Identifies that certificate was generated because the user is almost vaccinated
    // Last vaccination was too recent for another one
    VaccinationInsufficientHoursFromLastResult,
}
