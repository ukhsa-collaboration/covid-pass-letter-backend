// ReSharper disable ClassNeverInstantiated.Global
namespace CovidLetter.Backend.Common.Application.Certificates;

public class IntlRecoveryResponse : TestResultNhs, IGenericBarcodeResponse
{
    public string QRCode { get; set; } = null!;
}
