using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab06Wiring : ILab06Wiring
{
    private readonly Lab02Wiring _lab02 = new();

    public IItemStore CreateStore() => _lab02.CreateStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    public Type PublisherType => typeof(Pokedex);

    public Action Subscribe(IItemStore store, Action<IItem> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var dex = Dex(store);
        dex.ItemAdded += handler;                 // += adds this function to the list
        return () => dex.ItemAdded -= handler;    // -= removes exactly this one
    }

    public IReadOnlyList<IItem> Where(IItemStore store, Func<IItem, bool> keep)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(keep);

        var kept = new List<IItem>();
        foreach (var item in Dex(store))
            if (keep(item))                       // the caller's rule, run once per item
                kept.Add(item);
        return kept;
    }

    public IReadOnlyList<string> Map(IItemStore store, Func<IItem, string> describe)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(describe);

        var lines = new List<string>(store.Count);
        foreach (var item in Dex(store))
            lines.Add(describe(item));
        return lines;
    }

    public void ForEach(IItemStore store, Action<IItem> action)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in Dex(store))
            action(item);
    }

    private static Pokedex Dex(IItemStore store) => store as Pokedex
        ?? throw new ArgumentException($"Lab 06 expects a Pokedex, not {store?.GetType().Name ?? "null"}.", nameof(store));
}
