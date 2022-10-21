namespace CovidLetter.Backend.Common.Utilities;

public record struct Tag<T>(T Value, int Index, bool IsFirst, bool IsLast);

public static class EnumerableExtensions
{
    public static IEnumerable<Tag<T>> TagFirstLast<T>(this IEnumerable<T> enumerable)
    {
        using var enumerator = enumerable.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            yield break;
        }

        var isFirst = true;
        var isLast = false;
        var index = 0;
        while (!isLast)
        {
            var current = enumerator.Current;
            isLast = !enumerator.MoveNext();
            yield return new Tag<T>(current, index++, isFirst, isLast);
            isFirst = false;
        }
    }
}
