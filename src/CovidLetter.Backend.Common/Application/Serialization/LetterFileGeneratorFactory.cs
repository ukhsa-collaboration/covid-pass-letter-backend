namespace CovidLetter.Backend.Common.Application.Serialization;

using CovidLetter.Backend.Common.Options;

public class LetterFileGeneratorFactory : IFileGeneratorFactory<Letter>
{
    private readonly FeatureToggle featureToggle;

    public LetterFileGeneratorFactory(FeatureToggle featureToggle)
    {
        this.featureToggle = featureToggle;
    }

    public FileGenerator<Letter> Create()
    {
        return new FileGenerator<Letter>()
            .Map("Forename", l => l.Forename)
            .Map("Surname", l => l.Surname)
            .Map("Title", l => l.Title)
            .Map("Address_Line_1", l => string.Join(@""",""", l.Address_Line_1))
            .Map("Address_Line_2", l => string.Join(@""",""", l.Address_Line_2))
            .Map("Address_Line_3", l => string.Join(@""",""", l.Address_Line_3))
            .Map("Address_Line_4", l => string.Join(@""",""", l.Address_Line_4))
            .Map("Post_Code", l => l.Post_Code)
            .Map("Date_of_birth", l => l.Date_of_birth, "dd/MM/yyyy")
            .Map("Alternate_Language", l => l.Alternate_Language)
            .Map("Accessibility_Needs", l => l.Accessibility_Needs)
            .Map("Vaccine_Friendly_Name", l => l.Vaccine_Friendly_Name)
            .Map("Vaccination_Manufacturer", l => l.Vaccination_Manufacturer)
            .Map("Location_Dose", l => l.Location_Dose)
            .Map("Date_of_Dose", l => l.Date_of_Dose, "dd/MM/yyyy")
            .Map("Vaccine_Batch_No", l => l.Vaccine_Batch_No)
            .Map("Vaccine_or_Prophylaxis", l => l.Vaccine_or_Prophylaxis)
            .Map("Vaccine_Brand", l => l.Vaccine_Brand)
            .Map("Vaccine_Market_Authorisation_holder", l => l.Vaccine_Market_Authorisation_holder)
            .Map("Dose_Number", l => l.Dose_Number)
            .Map("Country_of_Vaccination", l => l.Country_of_Vaccination)
            .Map("Certificate_Issuer", l => l.Certificate_Issuer)
            .Map("Vaccine_Numerator", l => l.Vaccine_Numerator)
            .Map("Vaccine_Denominator", l => l.Vaccine_Denominator)
            .Map("Vaccine_Product_Code", (l) => string.Empty /* REDACTED */)
            .Map("Vaccine_Count", l => l.Vaccine_Count)
            .Map("Date_Of_Positive_Test_Result", l => l.Date_Of_Positive_Test_Result, "O")
            .Map("Type_Of_Test", l => l.Type_Of_Test)
            .Map("Disease_Targeted", l => l.Disease_Targeted)
            .Map("Country_Of_Test", l => l.Country_Of_Test)
            .Map("Date_Valid_From", l => l.Date_Valid_From, "O")
            .Map("Date_Valid_To", l => l.Date_Valid_To, "O")
            .Map("UID", l => l.UID)
            .Map("Barcode", l => l.Barcode)
            .Map("2DBarcode", l => l.TwoDBarcode)
            .Map("NHS_Number", l => string.Empty /* REDACTED */)
            .Map("2DBarcode_End_Date", l => l.TwoDBarcode_End_Date, "dd MMM yyyy")
            .Map("LetterType", l => l.LetterType)
            .Map("Expiry_Date", l => l.Expiry_Date, "dd/MM/yy")
            .Map("Recovery_UVCI", l => l.RecoveryUvci)
            .Map("Recovery_Barcode", l => l.RecoveryBarcode)
            .Map("Recovery_Barcode_End_Date", l => l.RecoveryBarcodeEndDate, "O");
    }
}
