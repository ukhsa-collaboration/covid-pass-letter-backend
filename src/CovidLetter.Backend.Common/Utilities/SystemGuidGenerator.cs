namespace CovidLetter.Backend.Common.Utilities;

public class SystemGuidGenerator
    : IGuidGenerator
{
    public Guid NewGuid()
    {
        return Guid.NewGuid();
    }
}
