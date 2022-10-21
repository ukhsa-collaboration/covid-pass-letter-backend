namespace CovidLetter.Backend.Common.Options;

using Microsoft.Extensions.Options;

public class FeatureToggle
{
    public const string BypassExclusionPeriod = nameof(BypassExclusionPeriod);

    public const string UnattendedRecoveryApiDisabled = nameof(UnattendedRecoveryApiDisabled);

    private readonly Lazy<IReadOnlySet<string>> enabledFeatures;

    public FeatureToggle(IOptions<FunctionOptions> functionOptions)
    {
        this.enabledFeatures = new Lazy<IReadOnlySet<string>>(() => NormalizeNames(functionOptions.Value.EnabledFeatures));
    }

    public bool IsEnabled(string featureName) => this.enabledFeatures.Value.Contains(featureName);

    private static HashSet<string> NormalizeNames(string? enabledFunctions)
    {
        return
            (enabledFunctions ?? string.Empty)
            .Split(",").Select(f => f.Trim())
            .ToHashSet(StringComparer.InvariantCultureIgnoreCase);
    }
}
