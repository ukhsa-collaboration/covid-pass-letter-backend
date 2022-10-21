namespace CovidLetter.Backend.Common.Application.Certificates;

using System.Globalization;
using CovidLetter.Backend.Common.Utilities;
using Hl7.Fhir.Model.R4;

public static class PatientExtensions
{
    public static bool Is5To11YearsOld(this Patient patient, IClock clock)
    {
        if (!DateTime.TryParseExact(
                patient.BirthDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateOfBirth))
        {
            return false;
        }

        var age = clock.GetAge(dateOfBirth);
        return age is >= 5 and < 12;
    }
}
