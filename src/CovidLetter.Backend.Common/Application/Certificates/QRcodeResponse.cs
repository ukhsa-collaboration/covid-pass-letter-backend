namespace CovidLetter.Backend.Common.Application.Certificates;

public class QrCodeResponse<T>
    where T : IGenericBarcodeResponse
{
    public string ValidityEndDate { get; set; } = null!;

    public string EligibilityEndDate { get; set; } = null!;

    public string UniqueCertificateIdentifier { get; set; } = null!;

    public T[] ResultData { get; set; } = null!;
}
