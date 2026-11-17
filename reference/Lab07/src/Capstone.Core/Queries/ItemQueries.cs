using SharpQuest.Contracts;

namespace Capstone.Core.Queries;

/// <summary>
/// Questions about a sequence of items, written as extension methods.
/// <para>
/// This is all LINQ is: static methods whose first parameter is the sequence, with <c>this</c>
/// in front of it so they read like members of a type nobody owns. <c>Where</c>, <c>Select</c>
/// and <c>OrderBy</c> live in exactly such a class — <c>System.Linq.Enumerable</c>.
/// </para>
/// </summary>
public static class ItemQueries
{
    /// <summary>
    /// The <paramref name="n"/> items with the longest display names, longest first,
    /// ties broken by Id. Fewer than n items simply yields all of them.
    /// </summary>
    public static IEnumerable<IItem> Longest(this IEnumerable<IItem> items, int n)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return items
            .OrderByDescending(item => item.DisplayName.Length)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Take(n);
    }

    /// <summary>The display names, in the sequence's own order, as one line.</summary>
    public static string OneLine(this IEnumerable<IItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return string.Join(", ", items.Select(item => item.DisplayName));
    }

    /// <summary>True when there is nothing to show. Reads better than <c>!items.Any()</c>.</summary>
    public static bool IsEmpty(this IEnumerable<IItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return !items.Any();
    }
}
