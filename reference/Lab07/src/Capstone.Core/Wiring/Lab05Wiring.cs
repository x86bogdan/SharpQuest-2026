using Capstone.Core.Collections;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab05Wiring : ILab05Wiring
{
    private readonly Lab02Wiring _lab02 = new();

    public IItemStore CreateStore() => _lab02.CreateStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    public Type CollectionType => typeof(Pokedex);

    public Type GenericDefinition => typeof(RecentlyUsed<>);

    public IItem At(IItemStore store, int index) => Dex(store)[index];

    public IItem? ById(IItemStore store, string id) => Dex(store)[id];

    public IReadOnlyList<IItem> EachInOrder(IItemStore store)
    {
        var items = new List<IItem>(store.Count);
        foreach (var item in Dex(store))   // foreach over the store itself, not over All()
            items.Add(item);
        return items;
    }

    public IEnumerable<IItem> Matching(IItemStore store, string text) => Dex(store).Matching(text);

    public IReadOnlyList<T> Recent<T>(int capacity, params T[] pushed)
    {
        var recent = new RecentlyUsed<T>(capacity);
        foreach (var value in pushed) recent.Push(value);
        return recent.Items().ToList();
    }

    private static Pokedex Dex(IItemStore store) => store as Pokedex
        ?? throw new ArgumentException($"Lab 05 expects a Pokedex, not {store?.GetType().Name ?? "null"}.", nameof(store));
}
