namespace CovidLetter.Backend.Common.Application;

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using CovidLetter.Backend.Common.Utilities;

/// <summary>
/// Warning: The value are stored in the database, so cannot change.
/// They are out of order in this enum in order to group by territory.
/// </summary>
public enum FileType
{
    SuccessLetterGb = 1,
    SuccessSpecialPrintLetterGb = 2,
    FailureLetterGb = 5,
    FailureSpecialPrintLetterGb = 6,

    SuccessLetterIm = 3,
    SuccessSpecialPrintLetterIm = 4,
    FailureLetterIm = 7,
    FailureSpecialPrintLetterIm = 8,

    SuccessLetterWales = 9,
    SuccessSpecialPrintLetterWales = 10,
    FailureLetterWales = 11,
    FailureSpecialPrintLetterWales = 12,
}

public enum FileCountry
{
    England,
    IsleOfMan,
    Wales,
}

public class FileNameTemplate
{
    private const string LetterGbPrefix = "NHSD_POST_VAX";
    private const string SpecialPrintLetterGbPrefix = "SPECIAL_PRINT_NHSD_POST_VAX";
    private const string LetterImPrefix = "IM_NHSD_POST_VAX";
    private const string SpecialPrintLetterImPrefix = "IM_SPECIAL_PRINT_NHSD_POST_VAX";
    private const string LetterWalesPrefix = "WALES_NHSD_POST_VAX";
    private const string SpecialPrintLetterWalesPrefix = "WALES_SPECIAL_PRINT_NHSD_POST_VAX";
    private const string FailureSuffix = "_FAILURE";

    public static readonly IReadOnlyCollection<FileNameTemplate> AllTemplates = new FileNameTemplate[]
    {
        new(FileType.SuccessLetterGb, LetterGbPrefix, FileCountry.England, false, false),
        new(FileType.SuccessSpecialPrintLetterGb, SpecialPrintLetterGbPrefix, FileCountry.England, true, false),
        new(FileType.FailureLetterGb, $"{LetterGbPrefix}{FailureSuffix}", FileCountry.England, false, true),
        new(FileType.FailureSpecialPrintLetterGb, $"{SpecialPrintLetterGbPrefix}{FailureSuffix}", FileCountry.England, true, true),

        new(FileType.SuccessLetterIm, LetterImPrefix, FileCountry.IsleOfMan, false, false),
        new(FileType.SuccessSpecialPrintLetterIm, SpecialPrintLetterImPrefix, FileCountry.IsleOfMan, true, false),
        new(FileType.FailureLetterIm, $"{LetterImPrefix}{FailureSuffix}", FileCountry.IsleOfMan, false, true),
        new(FileType.FailureSpecialPrintLetterIm, $"{SpecialPrintLetterImPrefix}{FailureSuffix}", FileCountry.IsleOfMan, true, true),

        new(FileType.SuccessLetterWales, LetterWalesPrefix, FileCountry.Wales, false, false),
        new(FileType.SuccessSpecialPrintLetterWales, SpecialPrintLetterWalesPrefix, FileCountry.Wales, true, false),
        new(FileType.FailureLetterWales, $"{LetterWalesPrefix}{FailureSuffix}", FileCountry.Wales, false, true),
        new(FileType.FailureSpecialPrintLetterWales, $"{SpecialPrintLetterWalesPrefix}{FailureSuffix}", FileCountry.Wales, true, true),
    };

    private static readonly IReadOnlyDictionary<FileType, FileNameTemplate> ByFileType =
        AllTemplates.ToDictionary(t => t.FileType);

    public FileNameTemplate(
        FileType fileType,
        string prefix,
        FileCountry country,
        bool isSpecialPrint,
        bool isFailureFile)
    {
        this.FileType = fileType;
        this.Prefix = prefix;
        this.Country = country;
        this.IsSpecialPrint = isSpecialPrint;
        this.IsFailureFile = isFailureFile;
    }

    public FileType FileType { get; }

    public string Prefix { get; }

    public FileCountry Country { get; }

    public bool IsSpecialPrint { get; }

    public bool IsFailureFile { get; }

    public static FileNameTemplate For(FileType type) => ByFileType[type];

    public static FileNameTemplate For(FileTypeRequest type)
    {
        var needsSpecialPrint = type.HasAccessibilityNeeds || type.HasAlternativeLanguage;
        return AllTemplates.Single(
            t => t.Country == type.Country &&
                 t.IsFailureFile == type.IsFailureFile &&
                 t.IsSpecialPrint == needsSpecialPrint);
    }

    public static bool TryFor(FileType type, [NotNullWhen(true)] out FileNameTemplate? template) =>
        ByFileType.TryGetValue(type, out template);

    public static bool IsEnrichable(string path)
    {
        return TryParse(path, true, out _);
    }

    public static bool TryParse(string path, bool forEnrichment, [NotNullWhen(true)] out FileNameTemplate? template)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            template = null;
            return false;
        }

        var matches = Regex.Match(
            CloudPath.GetFileName(path),
            "^(?<prefix>[a-zA-Z_]+)_(?<date>[0-9]+)_(?<time>[0-9]+)_FILE_(?<num>[0-9]+)_OF_(?<count>[0-9]+).(?<ext>(json)|(csv))$",
            RegexOptions.IgnoreCase);

        if (!matches.Success)
        {
            template = null;
            return false;
        }

        var prefix = matches.Groups["prefix"].Value;
        template = AllTemplates.SingleOrDefault(t => t.MatchesPrefix(prefix));
        if (template == null)
        {
            return false;
        }

        // templates containing `_FAILURE` are valid for LetterRequest processing but not Enrichment (legacy)
        if (forEnrichment
            && (template.Prefix.Contains(FailureSuffix, StringComparison.OrdinalIgnoreCase)
                || matches.Groups["ext"].Value.Equals("json", StringComparison.OrdinalIgnoreCase)))
        {
            template = null;
            return false;
        }

        return true;
    }

    public string RenderFileNameWithCsvExtension(DateTime date, int num, int count)
    {
        return this.RenderFileNameWithExtension(
            date,
            num,
            count,
            "csv");
    }

    public string RenderFileNameWithExtension(DateTime date, int num, int count, string fileNameExtension)
    {
        return string.Format(
            this.Prefix + "_{0}_FILE_{1:D2}_OF_{2:D2}." + fileNameExtension.Trim(),
            date.ToString("ddMMyy_HHmmss"),
            num,
            count);
    }

    public string RenderFileNameWithJsonExtension(DateTime date, int num, int count)
    {
        return this.RenderFileNameWithExtension(
            date,
            num,
            count,
            "json");
    }

    public bool MatchesPrefix(string prefix)
    {
        return string.Equals(prefix, this.Prefix, StringComparison.InvariantCultureIgnoreCase);
    }
}
