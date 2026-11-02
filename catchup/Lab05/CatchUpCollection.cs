// ─────────────────────────────────────────────────────────────────────────────
// Catch-up kit — Lab 05, collections.
//
// Copy this file into src/Capstone.Core/CatchUp/ ONLY if you missed session 5 (or didn't
// finish it) and a later lab needs your store to behave like a collection. Lab 07 is the one
// that does: its Names query has to be deferred, so it must query the store itself and not
// the snapshot All() hands you. With the kit:
//
//     using Capstone.Core.CatchUp;
//
//     store.AsCollection().Select(item => item.DisplayName)   // live: reads the store when enumerated
//     foreach (var item in store.AsCollection()) { … }
//     store.AsCollection()[0]                                 // by position, in the order items were added
//     store.AsCollection()["some-id"]                         // by Id, or null
//
// It works on any IItemStore, so your own store stays exactly as it is. It is deliberately plain,
// it is not your theme's code, and it earns you nothing for Lab 05 — the contract tests know which
// namespace it lives in, and a Lab 05 wiring or collection that leans on it is reported by name.
// To get those points, make your own store a collection (IEnumerable<IItem>, an iterator written
// with yield return, the two indexers) and stop using the kit.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using SharpQuest.Contracts;

namespace Capstone.Core.CatchUp;

/// <summary>
/// A live, read-only view of any store as a collection: <c>foreach</c>, LINQ, and the two indexers.
/// Nothing is copied when you create it. Every enumeration and every lookup asks the store
/// at that moment, so an item added after you made the view is still seen.
/// </summary>
public sealed class CatchUpCollection : IEnumerable<IItem>
{
    private readonly IItemStore _store;

    public CatchUpCollection(IItemStore store) =>
        _store = store ?? throw new ArgumentNullException(nameof(store));

    public int Count => _store.Count;

    /// <summary>By position, in the order items were added. Off either end is a programming error.</summary>
    public IItem this[int index]
    {
        get
        {
            var items = _store.All();
            if (index < 0 || index >= items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"The store holds {items.Count} item(s); positions run from 0 to {items.Count - 1}.");
            return items[index];
        }
    }

    /// <summary>By Id. Null when nothing has that Id: the same answer as FindById.</summary>
    public IItem? this[string id] => _store.FindById(id);

    /// <summary>
    /// Reads the store when enumeration starts, not when the view was made. Each enumeration
    /// walks its own snapshot, so adding to the store mid-loop doesn't throw.
    /// </summary>
    public IEnumerator<IItem> GetEnumerator() => _store.All().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>The one-word way in: <c>store.AsCollection()</c>.</summary>
public static class CatchUpCollectionExtensions
{
    public static CatchUpCollection AsCollection(this IItemStore store) => new(store);
}
