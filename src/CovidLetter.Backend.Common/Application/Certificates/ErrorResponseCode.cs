namespace CovidLetter.Backend.Common.Application.Certificates;

public enum ErrorResponseCode
{
    /// <summary>
    ///     Unknown error. See <see cref="Exception.InnerException" /> for details.
    /// </summary>
    UnknownError = 0,

    /// <summary>
    ///     Unhandled exception from within Unattended API.
    /// </summary>
    Unknown = 1,

    /// <summary>
    ///     Payload failed to parse as FHIR Patient
    /// </summary>
    PayloadFailedToParseAsFhirPatient = 10,

    /// <summary>
    ///     Patient has no identifier containing NHS-number
    /// </summary>
    PatientHasNoIdentifierContainingNhsNumber = 11,

    /// <summary>
    ///     NHS-number does not consist solely of digits
    /// </summary>
    NhsNumberDoesNotConsistSolelyOfDigits = 12,

    /// <summary>
    ///     Patient has no name populated with given name and family name
    /// </summary>
    PatientHasNoNamePopulatedWithGivenNameAndFamilyName = 13,

    /// <summary>
    ///     Patient has no birthdate
    /// </summary>
    PatientHasNoBirthdate = 14,

    /// <summary>
    ///     Patient is too young to obtain requested pass (age limit 5 for travel passes and 18 for domestic pass)
    /// </summary>
    PatientIsTooYoungToObtainRequestedPass = 15,

    /// <summary>
    ///     No vaccine records found.
    /// </summary>
    /// <remarks>
    ///     This happens if 0 records were found when looking up the citizen’s data in Immunization History API.
    /// </remarks>
    NoVaccineRecordsFound = 201,

    /// <summary>
    ///     No certificate generated due to vaccination history not meeting criteria.
    /// </summary>
    /// <remarks>
    ///     This happens if rule engine does not return error code 2 or 3 (which will map to error codes 102 and 103),
    ///     but does not return any valid certificate, yet we see at least one vaccine record when looking up the
    ///     citizen's data in Immunization History API.
    /// </remarks>
    NoCertificateGeneratedDueToVaccinationHistoryNotMeetingCriteria = 202,

    /// <summary>
    ///     No test results granting recovery was found.
    /// </summary>
    /// <remarks>
    ///     The user may or may not have test results, but the user was not granting a recovery certificate based on the
    ///     test results.
    /// </remarks>
    NoTestResultsGrantingRecoveryFound = 206,

    /// <summary>
    ///     Illegal value passed in query parameter allowPrimaryDoseCertificates (e.g any value other than "true" or
    ///     "false").
    /// </summary>
    IllegalValuePassed = 210,

    /// <summary>
    ///     No records found.
    /// </summary>
    /// <remarks>
    ///     This happens if citizen was not found to have any test results or vaccination events in Test Results API or
    ///     Immunization History API and also citizen was not found to be exempt while doing lookup in Exemption API.
    /// </remarks>
    NoRecordsFound = 401,

    /// <summary>
    ///     No certificates generated due to event history not meeting criteria.
    /// </summary>
    /// <remarks>
    ///     This happens if rule engine does not return error code 2 or 3 (which will map to error codes 102 and 103), but does
    ///     not return any valid certificate, yet we see at least one test result or at least one vaccine record when looking
    ///     up the citizen’s data in Test Result API and Immunization History API.
    /// </remarks>
    NoCertificatesGeneratedDueToEventHistoryNotMeetingCriteria = 402,
}
