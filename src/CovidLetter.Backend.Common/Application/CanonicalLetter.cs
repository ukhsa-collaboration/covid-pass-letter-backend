namespace CovidLetter.Backend.Common.Application;

using System.Collections.Immutable;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Application.Notifications;
using CovidLetter.Backend.Common.Utilities;

public class CanonicalLetter
{
    private List<VaccineTravelPass> vaccineTravelPasses = new(ImmutableList<VaccineTravelPass>.Empty);

    public Guid Id { get; set; }

    public string AppId { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }

    public FileType FileType { get; set; }

    public IReadOnlyCollection<string> LetterType { get; set; } = ImmutableList<string>.Empty;

    public Requestor Requestor { get; set; } = null!;

    public IReadOnlyCollection<VaccineTravelPass> VaccineTravelPasses
    {
        get => this.vaccineTravelPasses.OrderByDescending(vtp => vtp.VaccinationEvent.DateOfDose).ToList();
        set => this.vaccineTravelPasses = value.ToList();
    }

    public IReadOnlyCollection<RecoveryPass>? RecoveryPasses { get; set; } = ImmutableList<RecoveryPass>.Empty;

    public LetterWrapper<Letter> ToLetterWrapper(IClock clock)
    {
        IReadOnlyCollection<T> GetTravelPassDetail<T>(
            Func<VaccineTravelPass, T> selector,
            bool redactIf5To11 = false,
            T redactedValue = default!)
        {
            var redact = redactIf5To11 && this.Requestor.Is5To11YearsOld(clock);
            return this.VaccineTravelPasses.Select(p => redact ? redactedValue : selector(p)).ToList();
        }

        T? GetSingleRecoveryPassDetail<T>(Func<RecoveryPass, T> selector)
        {
            return this.RecoveryPasses?.Any() == true
                ? selector(this.RecoveryPasses.Single())
                : default;
        }

        var letter = new Letter
        {
            Forename = this.Requestor.Forename,
            Surname = this.Requestor.Surname,
            Title = this.Requestor.GetPrefixOrTitle(clock),
            Address_Line_1 = this.Requestor.AddressLine1,
            Address_Line_2 = this.Requestor.AddressLine2,
            Address_Line_3 = this.Requestor.AddressLine3,
            Address_Line_4 = this.Requestor.AddressLine4,
            Post_Code = this.Requestor.PostCode,
            Date_of_birth = this.Requestor.DateOfBirth,
            Alternate_Language = this.Requestor.AlternateLanguage,
            Accessibility_Needs = this.Requestor.AccessibilityNeeds,
            Vaccine_Friendly_Name = GetTravelPassDetail(p => p.VaccinationEvent.VaccineFriendlyName),
            Vaccination_Manufacturer = GetTravelPassDetail(p => p.VaccinationEvent.VaccineManufacturer.Display),
            Location_Dose = GetTravelPassDetail(p => p.VaccinationEvent.LocationDose, true, "-"),
            Date_of_Dose = GetTravelPassDetail(p => (DateTime?)p.VaccinationEvent.DateOfDose),
            Vaccine_Batch_No = GetTravelPassDetail(p => p.VaccinationEvent.VaccineBatchNumber),
            Vaccine_or_Prophylaxis = GetTravelPassDetail(p => p.VaccinationEvent.VaccineOrProphylaxis.Display),
            Vaccine_Brand = GetTravelPassDetail(p => p.VaccinationEvent.VaccineBrand.Display),
            Vaccine_Market_Authorisation_holder = ImmutableList<string>.Empty, // Withdrawn
            Dose_Number = ImmutableList<string>.Empty, // Withdrawn
            Country_of_Vaccination = GetTravelPassDetail(p => p.VaccinationEvent.CountryOfVaccination),
            Certificate_Issuer =
                GetTravelPassDetail(p => p.VaccinationEvent.CertificateIssuer).Distinct().SingleOrDefault() ?? string.Empty,
            Vaccine_Numerator = GetTravelPassDetail(p => (int?)p.VaccinationEvent.VaccineNumerator),
            Vaccine_Denominator = GetTravelPassDetail(p => (int?)p.VaccinationEvent.VaccineDenominator),
            Vaccine_Product_Code = ImmutableList<string>.Empty, // Withdrawn,
            Vaccine_Count = this.VaccineTravelPasses.Count,
            Date_Of_Positive_Test_Result = GetSingleRecoveryPassDetail(r => (DateTime?)r.DateOfPositiveTestResult),
            Type_Of_Test = GetSingleRecoveryPassDetail(r => r.TypeOfTest) ?? string.Empty,
            Disease_Targeted = GetSingleRecoveryPassDetail(r => r.DiseaseTargeted.Display) ?? string.Empty,
            Country_Of_Test = GetSingleRecoveryPassDetail(r => r.CountryOfTest) ?? string.Empty,
            Date_Valid_From = GetSingleRecoveryPassDetail(r => (DateTime?)r.DateValidFrom),
            Date_Valid_To = GetSingleRecoveryPassDetail(r => (DateTime?)r.DateValidTo),
            UID = string.Empty,
            Barcode = this.VaccineTravelPasses.FirstOrDefault()?.Uvci ?? string.Empty,
            TwoDBarcode = GetTravelPassDetail(p => p.QrCode),
            NHS_Number = string.Empty, // Withdrawn
            TwoDBarcode_End_Date = GetTravelPassDetail(p => p.QrCodeExpiry),
            LetterType = this.LetterType,
            RecoveryBarcode = GetSingleRecoveryPassDetail(r => r.RecoveryBarcode) ?? string.Empty,
            RecoveryBarcodeEndDate = GetSingleRecoveryPassDetail(r => (DateTime?)r.RecoveryBarcodeEndDate),
            RecoveryUvci = GetSingleRecoveryPassDetail(r => r.RecoveryUvci) ?? string.Empty,
        };

        return new LetterWrapper<Letter>(this.Id, this.AppId, this.CreatedOn, this.FileType, letter);
    }

    public LetterWrapper<FailureLetter> ToFailureLetterWrapper(IClock clock, NotificationReasonCode reasonCode)
    {
        var failure = new FailureLetter
        {
            Id = this.Id,
            AppId = this.AppId,
            CreatedOn = this.CreatedOn,
            FileType = this.FileType,
            Forename = this.Requestor.Forename,
            Surname = this.Requestor.Surname,
            Title = this.Requestor.GetPrefixOrTitle(clock),
            AddressLine1 = this.Requestor.AddressLine1,
            AddressLine2 = this.Requestor.AddressLine2,
            AddressLine3 = this.Requestor.AddressLine3,
            AddressLine4 = this.Requestor.AddressLine4,
            PostCode = this.Requestor.PostCode,
            AlternateLanguage = this.Requestor.AlternateLanguage,
            AccessibilityNeeds = this.Requestor.AccessibilityNeeds,
            ReasonCode = (int)reasonCode,
            ReasonText = FailureNotification.GetReasonText(reasonCode),
        };

        return new LetterWrapper<FailureLetter>(this.Id, this.AppId, this.CreatedOn, this.FileType, failure);
    }

    public static CanonicalLetter FromLetterRequest(CanonicalLetterRequest request)
    {
        return new CanonicalLetter
        {
            Id = request.Id,
            AppId = request.MessageId,
            CreatedOn = request.CreatedOn,
            FileType = FileNameTemplate.For(
                new FileTypeRequest(
                    CanonicalLetterRequest.RegionToCountry(request.LetterRequest.Region),
                    request.LetterRequest.HasAccessibilityNeeds,
                    request.LetterRequest.HasAlternateLanguage,
                    request.IsRejection)).FileType,
            LetterType = request.LetterTypes ?? new List<string>(),
            Requestor = new Requestor
            {
                Forename = request.LetterRequest.FirstName,
                Surname = request.LetterRequest.LastName,
                Title = string.Empty, // intentionally not mapped from letterRequest.Title
                AddressLine1 = new[] { request.LetterRequest.AddressLine1 },
                AddressLine2 = new[] { request.LetterRequest.AddressLine2 },
                AddressLine3 = new[] { request.LetterRequest.AddressLine3 },
                AddressLine4 = new[] { request.LetterRequest.AddressLine4 },
                PostCode = request.LetterRequest.Postcode,
                DateOfBirth = request.LetterRequest.GetDateOfBirth(),
                AlternateLanguage = request.LetterRequest.HasAlternateLanguage ? request.LetterRequest.AlternateLanguage : string.Empty,
                AccessibilityNeeds = request.LetterRequest.HasAccessibilityNeeds ? request.LetterRequest.AccessibilityNeeds : string.Empty,
            },
            VaccineTravelPasses = request.TravelPasses ?? new List<VaccineTravelPass>(),
            RecoveryPasses = request.RecoveryPasses ?? new List<RecoveryPass>(),
        };
    }
}

public class Requestor
{
    public string Forename { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public IReadOnlyCollection<string?> AddressLine1 { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> AddressLine2 { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> AddressLine3 { get; set; } = ImmutableList<string?>.Empty;

    public IReadOnlyCollection<string?> AddressLine4 { get; set; } = ImmutableList<string?>.Empty;

    public string PostCode { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string AlternateLanguage { get; set; } = string.Empty;

    public string AccessibilityNeeds { get; set; } = string.Empty;

    public bool Is5To11YearsOld(IClock clock)
    {
        if (this.DateOfBirth == null)
        {
            return false;
        }

        var age = clock.GetAge(this.DateOfBirth.Value);
        return age is >= 5 and < 12;
    }

    public string GetPrefixOrTitle(IClock clock)
    {
        return this.Is5To11YearsOld(clock) ? StringConsts.ToTheParentGuardianOf : this.Title;
    }
}

public class VaccineTravelPass
{
    public VaccinationEvent VaccinationEvent { get; set; } = null!;

    public string Uvci { get; set; } = string.Empty;

    public string QrCode { get; set; } = string.Empty;

    public DateTime QrCodeExpiry { get; set; }
}

public class VaccinationEvent
{
    public string VaccineFriendlyName { get; set; } = string.Empty;

    public CodedValue VaccineManufacturer { get; set; } = null!;

    public string LocationDose { get; set; } = string.Empty;

    public DateTime DateOfDose { get; set; }

    public string VaccineBatchNumber { get; set; } = string.Empty;

    public CodedValue VaccineOrProphylaxis { get; set; } = null!;

    public CodedValue VaccineBrand { get; set; } = null!;

    public int DoseNumber { get; set; }

    public string CountryOfVaccination { get; set; } = string.Empty;

    public string CertificateIssuer { get; set; } = string.Empty;

    public int VaccineNumerator { get; set; }

    public int VaccineDenominator { get; set; }

    public string VaccineProductCode { get; set; } = string.Empty;
}

public class RecoveryPass
{
    public DateTime DateOfPositiveTestResult { get; set; }

    public string TypeOfTest { get; set; } = string.Empty;

    public CodedValue DiseaseTargeted { get; set; } = null!;

    public string CountryOfTest { get; set; } = string.Empty;

    public DateTime DateValidFrom { get; set; }

    public DateTime DateValidTo { get; set; }

    public string RecoveryUvci { get; set; } = string.Empty;

    public string RecoveryBarcode { get; set; } = string.Empty;

    public DateTime RecoveryBarcodeEndDate { get; set; }
}

public class CodedValue
{
    public CodedValue()
    {
        this.Code = string.Empty;
        this.Display = string.Empty;
    }

    public CodedValue(string code, string display, string? codeSystem = null)
    {
        this.Code = code;
        this.Display = display;
        this.CodeSystem = codeSystem;
    }

    public string Code { get; set; }

    public string Display { get; set; }

    public string? CodeSystem { get; set; }
}

public class CanonicalLetterRequest
{
    public CanonicalLetterRequest(
        LetterRequest letterRequest,
        Guid id,
        string messageId,
        DateTime createdOn,
        IReadOnlyCollection<string>? letterTypes,
        IReadOnlyCollection<VaccineTravelPass>? travelPasses,
        IReadOnlyCollection<RecoveryPass>? recoveryPasses)
        : this(letterRequest, id, messageId, createdOn, letterTypes, travelPasses, recoveryPasses, false)
    {
    }

    public CanonicalLetterRequest(
        LetterRequest letterRequest,
        Guid id,
        string messageId,
        DateTime createdOn,
        bool isRejection)
        : this(letterRequest, id, messageId, createdOn, default!, default!, default!, isRejection)
    {
    }

    private CanonicalLetterRequest(
        LetterRequest letterRequest,
        Guid id,
        string messageId,
        DateTime createdOn,
        IReadOnlyCollection<string>? letterTypes,
        IReadOnlyCollection<VaccineTravelPass>? travelPasses,
        IReadOnlyCollection<RecoveryPass>? recoveryPasses,
        bool isRejection)
    {
        this.LetterRequest = letterRequest;
        this.Id = id;
        this.MessageId = messageId;
        this.CreatedOn = createdOn;
        this.LetterTypes = letterTypes;
        this.TravelPasses = travelPasses;
        this.RecoveryPasses = recoveryPasses;
        this.IsRejection = isRejection;
    }

    public LetterRequest LetterRequest { get; }

    public Guid Id { get; }

    public string MessageId { get; }

    public DateTime CreatedOn { get; }

    public IReadOnlyCollection<string>? LetterTypes { get; }

    public IReadOnlyCollection<VaccineTravelPass>? TravelPasses { get; }

    public IReadOnlyCollection<RecoveryPass>? RecoveryPasses { get; }

    public bool IsRejection { get; }

    public static FileCountry RegionToCountry(string region)
    {
        return region switch
        {
            "ENG" => FileCountry.England,
            "IM" => FileCountry.IsleOfMan,
            "WALES" => FileCountry.Wales,
            _ => throw new ArgumentException("Unknown region: " + region),
        };
    }
}
