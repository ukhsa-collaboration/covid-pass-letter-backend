namespace CovidLetter.Backend.Common.Application.Serialization;

using System.Globalization;
using System.Text;
using CovidLetter.Backend.Common.Utilities;

public class FileGenerator<TRow>
    where TRow : new()
{
    private readonly List<(string Title, Action<StringBuilder, TRow> Write)> columns = new();

    public string Generate(IReadOnlyCollection<TRow> rows)
    {
        var result = new StringBuilder();
        result.AppendJoin("|", this.columns.Select(c => c.Title)).Append('\n');

        foreach (var row in rows)
        {
            foreach (var (column, _, _, isLast) in this.columns.TagFirstLast())
            {
                column.Write(result, row);
                result.Append(!isLast ? '|' : '\n');
            }
        }

        result.Append($"Total_count_of_records|{rows.Count}");
        return result.ToString();
    }

    public FileGenerator<TRow> Map(string title, Func<TRow, string> getter)
    {
        return this.MapSingle(title, getter, x => x);
    }

    public FileGenerator<TRow> Map(string title, Func<TRow, IEnumerable<string?>> getter)
    {
        return this.MapList(title, getter, x => x);
    }

    public FileGenerator<TRow> Map(string title, Func<TRow, int?> getter)
    {
        return this.MapSingle(title, getter, x => x?.ToString(CultureInfo.InvariantCulture));
    }

    public FileGenerator<TRow> Map(string title, Func<TRow, IEnumerable<int?>> getter)
    {
        return this.MapList(title, getter, x => x?.ToString(CultureInfo.InvariantCulture));
    }

    public FileGenerator<TRow> Map(string title, Func<TRow, DateTime?> getter, string format)
    {
        return this.MapSingle(title, getter, x => x?.ToString(format, CultureInfo.InvariantCulture));
    }

    public FileGenerator<TRow> Map(string title, Func<TRow, DateTime> getter, string format)
    {
        return this.MapSingle(title, getter, x => x.ToString(format, CultureInfo.InvariantCulture));
    }

    public FileGenerator<TRow> Map(string title, Func<TRow, IEnumerable<DateTime?>> getter, string format)
    {
        return this.MapList(title, getter, x => x?.ToString(format, CultureInfo.InvariantCulture));
    }

    public FileGenerator<TRow> Map(string title, Func<TRow, IEnumerable<DateTime>> getter, string format)
    {
        return this.MapList(title, getter, x => x.ToString(format, CultureInfo.InvariantCulture));
    }

    private static string EscapeString(string? value)
    {
        return $"\"{value?.Replace(@"""", @"\""")}\"";
    }

    private FileGenerator<TRow> MapSingle<TValue>(
        string title,
        Func<TRow, TValue> getter,
        Func<TValue, string?> stringify)
    {
        return this.AddColumn(title, (sb, row) => sb.Append(EscapeString(stringify(getter(row)))));
    }

    private FileGenerator<TRow> MapList<TValue>(
        string title,
        Func<TRow, IEnumerable<TValue>> getter,
        Func<TValue, string?> stringify)
    {
        return this.AddColumn(title, (sb, row) =>
        {
            var stringValues = getter(row).Select(stringify).DefaultIfEmpty().Select(EscapeString);
            sb.AppendJoin(",", stringValues);
        });
    }

    private FileGenerator<TRow> AddColumn(string title, Action<StringBuilder, TRow> write)
    {
        this.columns.Add((title, write));
        return this;
    }
}
