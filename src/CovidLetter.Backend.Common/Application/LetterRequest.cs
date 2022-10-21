namespace CovidLetter.Backend.Common.Application;

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Utilities;

public class LetterRequest : IValidatableObject
{
    public static readonly string DomesticNhsx = "domesticnhsx";

    internal static readonly string VaccineLetter = "VaccineLetter";

    internal static readonly string Recovery = "Recovery";

    [Required]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "The NHS Number must be 10 digits.")]
    public string NhsNumber { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    [Required]
    public string DateOfBirth { get; set; } = default!;

    [Required]
    public string CorrelationId { get; set; } = default!;

    public string AddressLine1 { get; set; } = default!;

    public string AddressLine2 { get; set; } = default!;

    public string AddressLine3 { get; set; } = default!;

    public string AddressLine4 { get; set; } = default!;

    public string Postcode { get; set; } = default!;

    public string EmailAddress { get; set; } = default!;

    public string MobileNumber { get; set; } = default!;

    public string AlternateLanguage { get; set; } = default!;

    public string AccessibilityNeeds { get; set; } = default!;

    public string Region { get; set; } = default!;

    public ContactMethodType ContactMethod { get; set; }

    public string[] LetterType { get; set; } = default!;

    public string FullName => $"{this.FirstName} {this.LastName}".Trim();

    public bool HasAccessibilityNeeds =>
        !string.IsNullOrWhiteSpace(this.AccessibilityNeeds) &&
        !this.AccessibilityNeeds.Equals(StringConsts.NotRequested, StringComparison.InvariantCultureIgnoreCase);

    public bool HasAlternateLanguage =>
        !string.IsNullOrWhiteSpace(this.AlternateLanguage) &&
        !this.AlternateLanguage.Equals(StringConsts.NotRequested, StringComparison.InvariantCultureIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!this.TryGetDateOfBirth(out _))
        {
            yield return new ValidationResult("The field DateOfBirth must be a valid date.");
        }
    }

    public bool TryGetDateOfBirth(out DateTime dateOfBirth)
    {
        dateOfBirth = default;
        if (string.IsNullOrWhiteSpace(this.DateOfBirth))
        {
            return false;
        }

        return DateTime.TryParseExact(
            this.DateOfBirth,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out dateOfBirth);
    }

    public DateTime GetDateOfBirth()
    {
        if (this.TryGetDateOfBirth(out var result))
        {
            return result;
        }

        throw new InvalidOperationException("Date of birth is not a valid date");
    }

    public string GetNhsDobHash()
    {
        return Hash.GetHashValue(this.NhsNumber, this.GetDateOfBirth());
    }

    public int GetAge(IClock clock)
    {
        return clock.GetAge(this.GetDateOfBirth());
    }

    public bool IsVaccineLetter()
    {
        var letterType = this.LetterType;
        return letterType.Length != 0 && letterType.Any(t => string.Equals(t, VaccineLetter, StringComparison.OrdinalIgnoreCase));
    }
}
