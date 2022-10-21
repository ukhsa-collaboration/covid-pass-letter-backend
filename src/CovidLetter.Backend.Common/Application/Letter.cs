namespace CovidLetter.Backend.Common.Application;

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using CovidLetter.Backend.Common.Application.Serialization;
using CovidLetter.Backend.Common.Utilities;

public class Letter
{
    public string Forename { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public IReadOnlyCollection<string?> Address_Line_1 { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> Address_Line_2 { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> Address_Line_3 { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> Address_Line_4 { get; set; } = ImmutableList<string?>.Empty;

    public string Post_Code { get; set; } = string.Empty;

    public DateTime? Date_of_birth { get; set; }

    public string Alternate_Language { get; set; } = string.Empty;

    public string Accessibility_Needs { get; set; } = string.Empty;

    public IReadOnlyCollection<string?> Vaccine_Friendly_Name { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> Vaccination_Manufacturer { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> Location_Dose { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<DateTime?> Date_of_Dose { get; set; } = ImmutableList<DateTime?>.Empty;

    public IReadOnlyCollection<string?> Vaccine_Batch_No { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> Vaccine_or_Prophylaxis { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> Vaccine_Brand { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> Vaccine_Market_Authorisation_holder { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> Dose_Number { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> Country_of_Vaccination { get; set; } = ImmutableList<string?>.Empty;

    public string Certificate_Issuer { get; set; } = string.Empty;

    public IReadOnlyCollection<int?> Vaccine_Numerator { get; set; } = ImmutableList<int?>.Empty;

    public IReadOnlyCollection<int?> Vaccine_Denominator { get; set; } = ImmutableList<int?>.Empty;

    public IReadOnlyCollection<string?> Vaccine_Product_Code { get; set; } = ImmutableList<string?>.Empty;

    public int? Vaccine_Count { get; set; }

    public DateTime? Date_Of_Positive_Test_Result { get; set; }

    public string Type_Of_Test { get; set; } = string.Empty;

    public string Disease_Targeted { get; set; } = string.Empty;

    public string Country_Of_Test { get; set; } = string.Empty;

    public DateTime? Date_Valid_From { get; set; }

    public DateTime? Date_Valid_To { get; set; }

    public string UID { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public IReadOnlyCollection<string?> TwoDBarcode { get; set; } = ImmutableList<string?>.Empty;

    public string NHS_Number { get; set; } = string.Empty;

    public IReadOnlyCollection<DateTime> TwoDBarcode_End_Date { get; set; } = ImmutableList<DateTime>.Empty;

    public IReadOnlyCollection<string?> LetterType { get; set; } = ImmutableList<string?>.Empty;

    public DateTime? Expiry_Date { get; set; }

    public string RecoveryUvci { get; set; } = string.Empty;

    public string RecoveryBarcode { get; set; } = string.Empty;

    public DateTime? RecoveryBarcodeEndDate { get; set; }

    public string GetNhsDobHash()
    {
        return Hash.GetHashValue(this.NHS_Number, this.Date_of_birth.GetValueOrDefault());
    }
}
