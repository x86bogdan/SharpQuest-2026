// Lab 09 — A window
// The same model, the same store, and something you can click.
//
// Nothing from Labs 02–08 may break. Those checks run every time, as usual.

using System.ComponentModel;
using System.Windows.Input;

namespace SharpQuest.Contracts;

/// <summary>
/// The thing your window binds to.
///
/// <para><b>Why this is an interface and not a screenshot.</b> A window can't be graded by a test
/// runner on a build server — and a WPF project can't even be *built* on one, since CI runs on Linux.
/// So the part that gets checked automatically is the part that is ordinary C#: the view model.
/// The window itself is a required feature you show in the room.</para>
///
/// <para>That split isn't a workaround. It's MVVM, and it's the professional habit this lab exists
/// to build: everything about *what the screen shows* lives in a class that has never heard of a
/// Button, which is exactly what makes it testable. Your view model goes in
/// <c>Capstone.Core</c> — the tests check that too, along with the fact that your domain library
/// doesn't reference a UI framework at all.</para>
///
/// <para><b>Both frameworks are fine.</b> WPF on Windows, Avalonia anywhere. The XAML differs by a
/// namespace; none of what's below changes.</para>
/// </summary>
public interface ICatalogueViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// What the list in your window binds to.
    /// <list type="bullet">
    /// <item>It is an <b>observable</b> collection: it tells the UI when it changes
    ///       (<see cref="System.Collections.Specialized.INotifyCollectionChanged"/>).</item>
    /// <item>It is the <b>same object</b> every time — a binding holds on to the instance it was
    ///       given, so replacing the collection leaves the window showing the old one.</item>
    /// <item>It shows the store's items, in store order, narrowed by <see cref="Filter"/>.</item>
    /// <item>When something is added to the store, it appears here. That's Lab 06's event doing
    ///       the job it was written for.</item>
    /// </list>
    /// </summary>
    IEnumerable<IItem> Items { get; }

    /// <summary>
    /// What the user has typed in the search box. Setting it narrows <see cref="Items"/>
    /// (ignoring case) and raises <see cref="INotifyPropertyChanged.PropertyChanged"/>.
    /// Blank — or null — means everything. Setting it to the value it already has raises nothing.
    /// </summary>
    string Filter { get; set; }

    /// <summary>
    /// The row the user has selected, or null. Setting it raises <c>PropertyChanged</c> for itself
    /// and for <see cref="Details"/>, and changes what <see cref="RemoveCommand"/> will do.
    /// </summary>
    IItem? Selected { get; set; }

    /// <summary>
    /// One line about <see cref="Selected"/>, for the panel beside the list.
    /// Never blank — when nothing is selected it says so.
    /// </summary>
    string Details { get; }

    /// <summary>How many items are showing. Raises <c>PropertyChanged</c> when that changes.</summary>
    int Count { get; }

    /// <summary>
    /// The Add button. Its parameter is the item to add (your window would build one from a form;
    /// the tests hand it one). Adding goes through the store, so the store's rules still apply.
    /// </summary>
    ICommand AddCommand { get; }

    /// <summary>
    /// The Delete button. It removes <see cref="Selected"/> and ignores its parameter.
    /// <c>CanExecute</c> is false when nothing is selected — that's what greys the button out —
    /// and <c>CanExecuteChanged</c> is raised when the selection changes, or the button never
    /// notices. Executing with nothing selected does nothing; it doesn't throw.
    /// </summary>
    ICommand RemoveCommand { get; }
}

/// <summary>
/// Tells the Lab 09 tests about your view model.
/// Implement once, in src/Capstone.Core/Wiring/.
/// </summary>
public interface ILab09Wiring
{
    IItemStore CreateStore();

    /// <summary>A valid item; same contract as <see cref="ILab02Wiring.CreateSample"/>.</summary>
    IItem CreateSample(int seed);

    /// <summary>
    /// A new view model over this store. A fresh one each call — two windows over two stores
    /// share nothing.
    /// </summary>
    ICatalogueViewModel CreateViewModel(IItemStore store);
}
