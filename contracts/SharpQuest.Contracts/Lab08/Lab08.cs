// Lab 08 — Persistence & async
// Your capstone starts remembering things between runs, and `await` finally gets
// explained instead of copied.
//
// Nothing from Labs 02–07 may break. Those checks run every time, as usual.

namespace SharpQuest.Contracts;

/// <summary>
/// Tells the Lab 08 tests how your capstone saves and reloads itself.
/// Implement once, in src/Capstone.Core/Wiring/.
///
/// <para><b>The one rule that matters, and the reason this lab exists:</b></para>
/// <para>
/// What comes back has to be what went in — the same kinds, with the same values, in the same order.
/// Not "it didn't throw". The obvious version of this lab saves successfully, reloads without
/// complaint, and quietly loses every field except the Id and the display name, because it hands
/// the store object straight to <c>JsonSerializer</c> and <c>System.Text.Json</c> wrote the
/// *declared* element type. The tests below compare your items property by property, so that
/// particular disaster is a named failure rather than a discovery in week 11.
/// </para>
///
/// <para><b>Really asynchronous.</b> The tests read your compiled code: both methods must reach a
/// real <c>…Async</c> call in <c>System.IO</c> or <c>System.Text.Json</c>, and neither may block on
/// a task with <c>.Result</c> or <c>.Wait()</c>. Wrapping synchronous file calls in a <c>Task</c>
/// and handing it back is the thing this lab exists to talk you out of.</para>
/// </summary>
public interface ILab08Wiring
{
    IItemStore CreateStore();

    /// <summary>A valid item; same contract as <see cref="ILab02Wiring.CreateSample"/>.</summary>
    IItem CreateSample(int seed);

    /// <summary>Your kinds, from Lab 04. Same meaning as <see cref="ILab04Wiring.KindTypes"/>.</summary>
    IReadOnlyList<Type> KindTypes { get; }

    /// <summary>One item of that kind. Same meaning as <see cref="ILab04Wiring.CreateOfKind"/>.</summary>
    IItem CreateOfKind(int kindIndex, int seed);

    /// <summary>
    /// Writes everything the store holds to <paramref name="path"/>, replacing whatever was there.
    /// <list type="bullet">
    /// <item>A folder in the path that doesn't exist yet is created — asking a user to mkdir first is rude.</item>
    /// <item>The file is a snapshot: changing the store afterwards doesn't change the file.</item>
    /// <item>A null store, or a null or blank path, is a programming error
    ///       (<see cref="ArgumentNullException"/> / <see cref="ArgumentException"/>).</item>
    /// </list>
    /// </summary>
    Task SaveAsync(IItemStore store, string path);

    /// <summary>
    /// The store as it was saved: same items, same kinds, same values, same order.
    /// <list type="bullet">
    /// <item><b>A file that isn't there is an empty store, not an error.</b> Nothing saved yet is a
    ///       normal Tuesday — the first run of every program you will ever write.</item>
    /// <item><b>A file that is there but isn't yours is an error:</b>
    ///       <see cref="InvalidDataException"/>. Missing data and corrupt data are different problems,
    ///       and quietly treating the second as the first is how people lose work.</item>
    /// <item>The store it hands back is a real, working store — Lab 02's rules still apply to it.</item>
    /// <item>A null or blank path is a programming error.</item>
    /// </list>
    /// </summary>
    Task<IItemStore> LoadAsync(string path);
}
