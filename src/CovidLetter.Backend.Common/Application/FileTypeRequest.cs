namespace CovidLetter.Backend.Common.Application;

public record FileTypeRequest(
    FileCountry Country,
    bool HasAccessibilityNeeds,
    bool HasAlternativeLanguage,
    bool IsFailureFile);
