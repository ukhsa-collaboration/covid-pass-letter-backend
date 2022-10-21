// ReSharper disable InconsistentNaming
namespace CovidLetter.Backend.Common.Application.Certificates;

public interface IGenericBarcodeResponse
{
    string QRCode { get; }
}
