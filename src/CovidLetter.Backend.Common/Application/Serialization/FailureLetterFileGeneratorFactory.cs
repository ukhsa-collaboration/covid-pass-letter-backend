namespace CovidLetter.Backend.Common.Application.Serialization;

internal class FailureLetterFileGeneratorFactory : IFileGeneratorFactory<FailureLetter>
{
    public FileGenerator<FailureLetter> Create() =>
        new FileGenerator<FailureLetter>()
            .Map("Forename", l => l.Forename)
            .Map("Surname", l => l.Surname)
            .Map("Address_Line_1", l => string.Join(@""",""", l.AddressLine1))
            .Map("Address_Line_2", l => string.Join(@""",""", l.AddressLine2))
            .Map("Address_Line_3", l => string.Join(@""",""", l.AddressLine3))
            .Map("Address_Line_4", l => string.Join(@""",""", l.AddressLine4))
            .Map("Post_Code", l => l.PostCode)
            .Map("Reason_Code", l => l.ReasonCode)
            .Map("Reason_Text", l => l.ReasonText)
            .Map("Alternate_Language", l => l.AlternateLanguage)
            .Map("Accessibility_Needs", l => l.AccessibilityNeeds)
            .Map("Title", l => l.Title);
}
