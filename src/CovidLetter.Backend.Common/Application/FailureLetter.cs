// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace CovidLetter.Backend.Common.Application;

using System.Collections.Immutable;

public class FailureLetter
{
    public Guid Id { get; set; }

    public string AppId { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }

    public FileType FileType { get; set; }

    public string Forename { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public IReadOnlyCollection<string?> AddressLine1 { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> AddressLine2 { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> AddressLine3 { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> AddressLine4 { get; set; } = ImmutableList<string?>.Empty;

    public string PostCode { get; set; } = string.Empty;

    public string AccessibilityNeeds { get; set; } = string.Empty;

    public int ReasonCode { get; set; }

    public string ReasonText { get; set; } = string.Empty;

    public string AlternateLanguage { get; set; } = string.Empty;
}
