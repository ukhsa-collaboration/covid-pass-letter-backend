namespace CovidLetter.Backend.Common.Application.Barcodes;

using Hl7.Fhir.Model;
using Hl7.Fhir.Model.R4;

public static class FhirMapper
{
    public static Patient MapRequest(LetterRequest request)
    {
        return new Patient
        {
            Identifier = new List<Identifier>
            {
                new("NHS-number", request.NhsNumber),
            },
            Name = new List<HumanName>
            {
                new()
                {
                    Family = request.LastName,
                    Given = new[] { request.FirstName },
                },
            },
            BirthDate = request.DateOfBirth,
        };
    }
}
