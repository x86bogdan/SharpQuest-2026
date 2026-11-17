using System.Collections;
using SharpQuest.Contracts;

namespace Capstone.Core.Storage;

/// <summary>
/// The store. A list keeps insertion order; a dictionary makes lookups fast.
/// Both have to agree, so every change goes through Add and Remove.
/// Lab 05: it is also a collection — foreach works on it, and it indexes by position and by Id.
/// Lab 06: it announces new entries, so anything that cares can listen without the Pokédex knowing about it.
/// </summary>
public sealed class Pokedex : IItemStore, IEnumerable<IItem>
{
    private readonly List<IItem> _ordered = [];
    private readonly Dictionary<string, IItem> _byId = new(StringComparer.Ordinal);

    /// <summary>
    /// Raised after an entry is really in. `event` (not a plain Action field) means only this class
    /// can raise it, and nobody outside can wipe the subscriber list.
    /// </summary>
    public event Action<IItem>? ItemAdded;

    public int Count => _ordered.Count;

    public void Add(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!_byId.TryAdd(item.Id, item))
            throw new ArgumentException($"An entry with Id '{item.Id}' is already in the Pokédex.", nameof(item));

        _ordered.Add(item);

        // After the change, never before: a subscriber that reads Count must see the new number.
        // `?.Invoke` because an event with nobody attached is null.
        ItemAdded?.Invoke(item);
    }

    public IItem? FindById(string id) => _byId.GetValueOrDefault(id);

    // ToArray copies, so callers get a snapshot rather than a live view of _ordered.
    public IReadOnlyList<IItem> All() => _ordered.ToArray();

    public bool Remove(string id)
    {
        if (!_byId.Remove(id, out var item))
            return false;

        _ordered.Remove(item);
        return true;
    }

    /// <summary>By position, in the order entries were added.</summary>
    public IItem this[int index] => _ordered[index];

    /// <summary>By Id. Null when nothing has that Id — same answer as FindById.</summary>
    public IItem? this[string id] => _byId.GetValueOrDefault(id);

    /// <summary>
    /// An iterator we wrote. `yield return` hands entries out one at a time, as the caller asks for them,
    /// so `foreach (var p in pokedex)` never builds a second list.
    /// </summary>
    public IEnumerator<IItem> GetEnumerator()
    {
        foreach (var item in _ordered)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Entries whose display name contains the text, in store order. Lazy, like GetEnumerator.</summary>
    public IEnumerable<IItem> Matching(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        foreach (var item in _ordered)
            if (item.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase))
                yield return item;
    }
}
