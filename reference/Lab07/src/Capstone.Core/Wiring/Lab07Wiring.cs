using Capstone.Core.Queries;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab07Wiring : ILab07Wiring
{
    private readonly Lab02Wiring _lab02 = new();

    public IItemStore CreateStore() => _lab02.CreateStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    public Type ExtensionHost => typeof(ItemQueries);

    /// <summary>
    /// A query, not a result: Select over the Pokédex itself — which is a collection, since Lab 05.
    /// Nothing is read until somebody enumerates it, so this answers about whatever is in the
    /// Pokédex at that moment. `All()` would have copied the list and frozen the answer.
    /// </summary>
    public IEnumerable<string> Names(IItemStore store) =>
        Dex(store).Select(item => item.DisplayName);

    public IReadOnlyList<IItem> Sorted(IEnumerable<IItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.Ordinal)   // equal names are a tie; the Id settles it
            .ToList();                                          // a finished list, not a query
    }

    public IReadOnlyList<IItem> Page(IEnumerable<IItem> items, int skip, int take)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegative(take);

        return Sorted(items).Skip(skip).Take(take).ToList();
    }

    /// <summary>One line, because the work is in the extension method where it belongs.</summary>
    public IReadOnlyList<IItem> Longest(IEnumerable<IItem> items, int n) => items.Longest(n).ToList();

    public StoreStats Summary(IEnumerable<IItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var names = items.Select(item => item.DisplayName).ToList();
        if (names.Count == 0)
            return new StoreStats(0, 0, null, 0);   // Average() on an empty sequence throws, so say what nothing means

        return new StoreStats(
            names.Count,
            names.Select(name => char.ToUpperInvariant(name[0])).Distinct().Count(),
            items.Longest(1).Single().DisplayName,
            names.Average(name => name.Length));
    }

    private static Pokedex Dex(IItemStore store) => store as Pokedex
        ?? throw new ArgumentException($"Lab 07 expects a Pokedex, not {store?.GetType().Name ?? "null"}.", nameof(store));
}
