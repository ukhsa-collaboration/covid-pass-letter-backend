namespace CovidLetter.Backend.Common.Utilities;

public interface IChaosMonkey
{
    void Poke(string? position = null);
}
