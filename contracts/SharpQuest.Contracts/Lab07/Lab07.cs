// Lab 07 — LINQ
// Last week you wrote code that takes a function and runs it over a collection.
// This week you find out that fifty of those already exist, that they chain,
// and that most of them don't do anything until you ask for the results.
//
// Nothing from Labs 02–06 may break. Those checks run every time, as usual.

namespace SharpQuest.Contracts;

/// <summary>
/// What a store looks like in one line. Every number is a LINQ aggregate.
/// <para>
/// An empty sequence answers <c>new StoreStats(0, 0, null, 0)</c> — note that
/// <c>Average()</c> on an empty sequence throws, so that case needs an answer of its own.
/// </para>
/// </summary>
/// <param name="Count">How many items there are.</param>
/// <param name="DistinctInitials">
/// How many different letters the display names start with, ignoring case
/// (<c>Pikachu</c> and <c>pidgey</c> share one).
/// </param>
/// <param name="LongestName">
/// The longest display name — the same one <c>Longest(items, 1)</c> returns. Null when there are no items.
/// </param>
/// <param name="AverageNameLength">The mean length of the display names. 0 when there are no items.</param>
public sealed record StoreStats(int Count, int DistinctInitials, string? LongestName, double AverageNameLength);

/// <summary>
/// Tells the Lab 07 tests about your five queries.
/// Implement once, in src/Capstone.Core/Wiring/.
///
/// <para><b>Two orderings are used throughout, and they are worth reading once:</b></para>
/// <list type="bullet">
/// <item><b>Name order</b> — by <c>DisplayName</c> ignoring case
///       (<c>StringComparer.OrdinalIgnoreCase</c>), ties broken by <c>Id</c>
///       (<c>StringComparer.Ordinal</c>). Two names that differ only in case are a tie.</item>
/// <item><b>Length order</b> — by <c>DisplayName.Length</c>, longest first,
///       ties broken by <c>Id</c> (<c>StringComparer.Ordinal</c>).</item>
/// </list>
///
/// <para>
/// The Lab 07 tests read your compiled code and check that each of the five queries really
/// goes through <c>System.Linq</c>, and that at least five different query methods appear
/// between them. A hand-written <c>foreach</c> that produces the right answer fails this lab:
/// today's point is that these are already written.
/// </para>
/// </summary>
public interface ILab07Wiring
{
    IItemStore CreateStore();

    /// <summary>A valid item; same contract as <see cref="ILab02Wiring.CreateSample"/>.</summary>
    IItem CreateSample(int seed);

    /// <summary>
    /// Every display name, in store order.
    /// <para>
    /// <b>Deferred.</b> Nothing is read until the caller enumerates the result, so an item added
    /// between the call and the <c>foreach</c> is included. That means querying the store itself —
    /// the collection you made it in Lab 05 — and not the snapshot <c>All()</c> hands you.
    /// It must also survive being enumerated twice.
    /// </para>
    /// </summary>
    IEnumerable<string> Names(IItemStore store);

    /// <summary>
    /// The items in <b>name order</b> (see the type remarks). A finished list, not a lazy query.
    /// The sequence you were given is not reordered, and nothing is added or dropped.
    /// A null sequence is a programming error: <see cref="ArgumentNullException"/>.
    /// </summary>
    IReadOnlyList<IItem> Sorted(IEnumerable<IItem> items);

    /// <summary>
    /// One page of <b>name order</b>: skip <paramref name="skip"/> items, then take
    /// <paramref name="take"/> of them. Past the end simply yields fewer, or none.
    /// <para>
    /// A negative <paramref name="skip"/> or <paramref name="take"/> is a programming error:
    /// <see cref="ArgumentOutOfRangeException"/>. (<c>Skip</c> and <c>Take</c> are quietly
    /// forgiving about negatives, which is exactly why this asks you to say what you mean.)
    /// </para>
    /// </summary>
    IReadOnlyList<IItem> Page(IEnumerable<IItem> items, int skip, int take);

    /// <summary>
    /// The <paramref name="n"/> items with the longest display names, in <b>length order</b>.
    /// Fewer than <paramref name="n"/> items simply yields all of them.
    /// <para>
    /// <b>Write this one as an extension method</b> on <c>IEnumerable&lt;IItem&gt;</c>, in the static
    /// class named by <see cref="ExtensionHost"/>, and call it from here. The tests call your
    /// extension directly and expect the same answer, so this method must be the extension's work.
    /// Either spelling counts: <c>static … Longest(this IEnumerable&lt;IItem&gt; items, int n)</c>,
    /// or the same method inside a C# 14 <c>extension(IEnumerable&lt;IItem&gt; items)</c> block.
    /// </para>
    /// <para>n = 0 yields nothing; a negative n throws <see cref="ArgumentOutOfRangeException"/>;
    /// a null sequence throws <see cref="ArgumentNullException"/>.</para>
    /// </summary>
    IReadOnlyList<IItem> Longest(IEnumerable<IItem> items, int n);

    /// <summary>
    /// The static class in <c>Capstone.Core</c> that holds your extension methods —
    /// the one declaring the <c>Longest</c> extension above.
    /// </summary>
    Type ExtensionHost { get; }

    /// <summary>
    /// The whole sequence in one line: see <see cref="StoreStats"/>.
    /// A null sequence throws <see cref="ArgumentNullException"/>.
    /// </summary>
    StoreStats Summary(IEnumerable<IItem> items);
}
