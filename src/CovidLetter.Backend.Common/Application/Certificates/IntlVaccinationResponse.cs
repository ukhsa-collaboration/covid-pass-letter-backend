// ReSharper disable ClassNeverInstantiated.Global
namespace CovidLetter.Backend.Common.Application.Certificates;

public class IntlVaccinationResponse : Vaccine, IGenericBarcodeResponse
{
    public string QRCode { get; set; } = null!;
}
