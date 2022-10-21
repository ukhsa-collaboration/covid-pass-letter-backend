namespace CovidLetter.Backend.Common.Utilities;

using System.ComponentModel.DataAnnotations;

public class ValidationHelpers
{
    public static bool TryValidate<T>(T instance, out List<ValidationResult> validationResults)
        where T : class
    {
        validationResults = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, new ValidationContext(instance), validationResults, true);
    }

    public static void Validate<T>(T instance)
        where T : class
    {
        if (!TryValidate(instance, out var results))
        {
            var summary = string.Join(Environment.NewLine, results.Select(r => r.ToString()));
            throw new ArgumentException($"Validation error(s)\n\n{summary}");
        }
    }
}
