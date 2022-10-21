namespace CovidLetter.Backend.Common.Application.Certificates;

public class Vaccine
{
    public int DoseNumber { get; set; }

    public DateTime VaccinationDate { get; set; }

    public Tuple<string, string> VaccineManufacturer { get; set; } = null!;

    public Tuple<string, string> DiseaseTargeted { get; set; } = null!;

    public Tuple<string, string> VaccineType { get; set; } = null!;

    public Tuple<string, string> Product { get; set; } = null!;

    public string VaccineBatchNumber { get; set; } = null!;

    public string CountryOfVaccination { get; set; } = null!;

    public string Authority { get; set; } = null!;

    public string Site { get; set; } = null!;

    public DateTime DateTimeOfTest => this.VaccinationDate;

    public string ValidityType => this.VaccineManufacturer.Item2;

    public int TotalSeriesOfDoses { get; set; }

    public string DisplayName { get; set; } = null!;

    public string SnomedCode { get; set; } = null!;

    public DateTime DateEntered { get; set; }

    public string CountryCode => this.CountryOfVaccination;

    public string ProcedureCode { get; set; } = null!;

    public bool IsBooster { get; set; }
}
