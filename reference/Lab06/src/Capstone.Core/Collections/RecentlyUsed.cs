namespace Capstone.Core.Collections;

/// <summary>
/// The last <see cref="Capacity"/> distinct values, newest first — a "recently viewed" list.
/// Generic because the rule has nothing to do with Pokémon: it works for ids, searches, trainers,
/// or anything else we later decide to remember.
/// </summary>
public sealed class RecentlyUsed<T>
{
    private readonly LinkedList<T> _values = new();
    private readonly IEqualityComparer<T> _comparer;

    public RecentlyUsed(int capacity, IEqualityComparer<T>? comparer = null)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Remembering fewer than one thing is not remembering.");

        Capacity = capacity;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public int Capacity { get; }

    public int Count => _values.Count;

    /// <summary>Puts a value at the front. A value already known moves there instead of being stored twice.</summary>
    public void Push(T value)
    {
        for (var node = _values.First; node is not null; node = node.Next)
        {
            if (!_comparer.Equals(node.Value, value)) continue;
            _values.Remove(node);
            break;
        }

        _values.AddFirst(value);

        if (_values.Count > Capacity)
            _values.RemoveLast();
    }

    /// <summary>What is remembered, newest first. Lazy: nothing is copied until somebody enumerates.</summary>
    public IEnumerable<T> Items()
    {
        for (var node = _values.First; node is not null; node = node.Next)
            yield return node.Value;
    }
}
