namespace CovidLetter.Backend.Common.Utilities;

using System.Runtime.CompilerServices;

public class Ensure
{
    public static void NotNull<T>(T argument, [CallerArgumentExpression("argument")] string? paramName = null)
        where T : class
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    public static void NotNullOrWhiteSpace(string argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (string.IsNullOrWhiteSpace(argument))
        {
            throw new ArgumentException("Value cannot be empty or white space.", paramName);
        }
    }
}
