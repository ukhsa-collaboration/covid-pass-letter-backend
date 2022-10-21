namespace CovidLetter.Backend.Common.Utilities;

public class NullChaosMonkey
    : IChaosMonkey
{
    public void Poke(string? position = null)
    {
    }
}
