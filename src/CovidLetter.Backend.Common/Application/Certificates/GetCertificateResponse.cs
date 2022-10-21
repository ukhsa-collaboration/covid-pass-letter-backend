namespace CovidLetter.Backend.Common.Application.Certificates;

public class GetCertificateResponse<T>
{
    private readonly T? value;
    private readonly ErrorResponseCode? responseCode;

    public GetCertificateResponse(T value)
    {
        this.value = value;
        this.responseCode = null;
    }

    public GetCertificateResponse(ErrorResponseCode code)
    {
        this.value = default;
        this.responseCode = code;
    }

    public bool IsSuccessful => this.responseCode == null;

    public T Response => this.IsSuccessful
        ? this.value!
        : throw new InvalidOperationException("Attempted to get response when it is unsuccessful");

    public ErrorResponseCode ErrorCode => !this.IsSuccessful
        ? this.responseCode!.Value
        : throw new InvalidOperationException("Attempted to get error code when it is successful");

    public static implicit operator GetCertificateResponse<T>(T value) => new(value);

    public static implicit operator GetCertificateResponse<T>(ErrorResponseCode code) => new(code);

    public GetCertificateResponse<TOther> MapIfSuccessful<TOther>(Func<T, TOther> successSelector)
    {
        if (this.IsSuccessful)
        {
            return new GetCertificateResponse<TOther>(successSelector(this.Response));
        }

        return new GetCertificateResponse<TOther>(this.ErrorCode);
    }
}
