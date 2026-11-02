using System.ComponentModel;
using System.Reflection;

namespace SharpQuest.Contracts.Tests.Lab09;

/// <summary>
/// Lab 09: telling the window. Binding is not magic — it is one event, and the window is listening
/// to it. A property that changes quietly is a label that never updates.
/// </summary>
public class NotifyTests : Lab09TestBase
{
    [Fact]
    public void The_view_model_declares_a_real_event()
    {
        var type = Wiring.CreateViewModel(Wiring.CreateStore()).GetType();

        var declared = type.GetEvents(BindingFlags.Public | BindingFlags.Instance)
            .Any(e => e.Name == nameof(INotifyPropertyChanged.PropertyChanged));

        Assert.True(declared || typeof(INotifyPropertyChanged).IsAssignableFrom(type),
            $"{type.Name} has no PropertyChanged event. It is the whole of how a window finds out anything changed.");
    }

    [Fact]
    public void Setting_the_filter_announces_it()
    {
        var (_, vm) = Open(1, 2);
        using var seen = new Announced(vm);

        vm.Filter = "something";

        Assert.True(seen.Saw(nameof(ICatalogueViewModel.Filter)),
            $"Filter was set and the view model announced {seen.What}.");
    }

    [Fact]
    public void Setting_the_selection_announces_it()
    {
        var (store, vm) = Open(1, 2);
        using var seen = new Announced(vm);

        vm.Selected = store.All()[0];

        Assert.True(seen.Saw(nameof(ICatalogueViewModel.Selected)),
            $"Selected was set and the view model announced {seen.What}.");
    }

    [Fact]
    public void The_details_follow_the_selection()
    {
        var (store, vm) = Open(1, 2);

        vm.Selected = store.All()[0];
        var first = vm.Details;
        vm.Selected = store.All()[1];

        Assert.True(first != vm.Details,
            $"Two different items both describe themselves as \"{first}\". Details should say something about " +
            "the item that is actually selected.");
    }

    [Fact]
    public void And_the_details_announce_themselves()
    {
        var (store, vm) = Open(1, 2);
        using var seen = new Announced(vm);

        vm.Selected = store.All()[1];

        Assert.True(seen.Saw(nameof(ICatalogueViewModel.Details)),
            $"The selection changed, so Details changed, but the view model announced {seen.What}. " +
            "A property that is computed from another one still has to be announced when that other one moves.");
    }

    [Fact]
    public void Setting_the_filter_to_what_it_already_is_announces_nothing()
    {
        var (_, vm) = Open(1, 2);
        vm.Filter = "same";
        using var seen = new Announced(vm);

        vm.Filter = "same";

        Assert.True(seen.Names.Count == 0,
            $"Setting Filter to the value it already had announced {seen.What}. Compare before you assign — " +
            "in a real window this is the difference between a list that redraws when it must and one that " +
            "redraws on every keystroke.");
    }

    [Fact]
    public void Setting_the_selection_to_what_it_already_is_announces_nothing()
    {
        var (store, vm) = Open(1, 2);
        vm.Selected = store.All()[0];
        using var seen = new Announced(vm);

        vm.Selected = store.All()[0];

        Assert.True(seen.Names.Count == 0, $"Re-selecting the same item announced {seen.What}.");
    }

    [Fact]
    public void There_is_always_something_to_show_in_the_details()
    {
        var (store, vm) = Open(1);

        Assert.False(string.IsNullOrWhiteSpace(vm.Details),
            "With nothing selected, Details is blank. Say so instead — an empty panel looks like a bug.");

        vm.Selected = store.All()[0];
        Assert.False(string.IsNullOrWhiteSpace(vm.Details), "With something selected, Details is blank.");
    }

    [Fact]
    public void Nothing_can_be_selected_again()
    {
        var (store, vm) = Open(1, 2);
        vm.Selected = store.All()[0];

        vm.Selected = null;

        Assert.Null(vm.Selected);
        Assert.False(string.IsNullOrWhiteSpace(vm.Details), "Deselecting left Details blank.");
    }
}
