// Lab 02 — Types & modelling
// The contract. Your capstone implements these; the Lab 02 tests check them.
// The nouns are yours: an IItem can be a book, a spin, an order, a Pokémon.

namespace SharpQuest.Contracts;

/// <summary>Anything your capstone stores and looks up.</summary>
public interface IItem
{
    /// <summary>Unique within a store, never empty, never changes after creation.</summary>
    string Id { get; }

    /// <summary>What a human would call it. Never empty.</summary>
    string DisplayName { get; }
}

/// <summary>Holds your items and finds them again. Session 5 grows this into a real collection.</summary>
public interface IItemStore
{
    int Count { get; }

    /// <summary>
    /// Adds an item.
    /// Throws <see cref="ArgumentNullException"/> for null,
    /// and <see cref="ArgumentException"/> if an item with the same Id is already stored.
    /// </summary>
    void Add(IItem item);

    /// <summary>The item with exactly this Id (case-sensitive), or null if there is none.</summary>
    IItem? FindById(string id);

    /// <summary>
    /// Every item, in the order it was added.
    /// A snapshot: adding or removing later does not change a list you already returned.
    /// </summary>
    IReadOnlyList<IItem> All();

    /// <summary>Removes the item with this Id. True if something was removed.</summary>
    bool Remove(string id);
}

/// <summary>Turns your items into JSON and back. This is where your DTOs live.</summary>
public interface IItemMapper
{
    /// <summary>A JSON object representing the item.</summary>
    string ToJson(IItem item);

    /// <summary>
    /// The item described by <paramref name="json"/>, or null if the text is not valid JSON
    /// or is missing something your model requires. Never throws for bad input.
    /// </summary>
    IItem? FromJson(string json);
}

/// <summary>Tells the Lab 02 tests how to make your objects. Implement once, in src/Capstone.Core/Wiring/.</summary>
public interface ILab02Wiring
{
    IItemStore CreateStore();

    IItemMapper CreateMapper();

    /// <summary>
    /// A valid item. The same seed always gives the same Id; different seeds give different Ids.
    /// Seeds from 1 to 100 are used.
    /// </summary>
    IItem CreateSample(int seed);
}
