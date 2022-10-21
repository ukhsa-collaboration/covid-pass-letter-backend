namespace CovidLetter.Backend.Common.Application.Certificates;

public class FailedToRetrieveCertificateException : Exception
{
    public FailedToRetrieveCertificateException(ErrorResponseCode errorCode)
    {
        this.ErrorCode = errorCode;
    }

    public ErrorResponseCode ErrorCode { get; }
}
