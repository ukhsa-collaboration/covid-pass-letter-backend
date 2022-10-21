namespace CovidLetter.Backend.Common.Application.Certificates.Domestic;

public class DomesticCertificateResponse
{
    public DomesticCertificate Certificate { get; set; } = null!;

    public bool CertificateEverExisted { get; set; }

    public ErrorResponseCode? ErrorCode { get; set; }

    public DateTime? WaitPeriod { get; set; }

    public TwoPassStatus TwoPassStatus { get; set; }
}
