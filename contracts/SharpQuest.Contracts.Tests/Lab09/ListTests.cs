using System.Collections.Specialized;

namespace SharpQuest.Contracts.Tests.Lab09;

/// <summary>
/// Lab 09: the list in the window. A binding holds on to one collection object and waits to be told
/// when it changes — which is why "observable" and "the same instance" are both checks.
/// </summary>
public class ListTests : Lab09TestBase
{
    [Fact]
    public void The_list_starts_with_what_the_store_holds()
    {
        var (store, vm) = Open(1, 2, 3);

        Assert.Equal(Ids(store.All()), Ids(Shown(vm)));
    }

    [Fact]
    public void The_list_is_observable()
    {
        var (_, vm) = Open(1);

        using var watch = new Watched(vm.Items);

        Assert.True(watch.IsObservable,
            $"Items is a {vm.Items.GetType().Name}, which has no way to tell the window it changed. " +
            "Use an ObservableCollection<T>: a plain List looks identical until something is added, " +
            "and then the window just sits there.");
    }

    [Fact]
    public void The_list_is_the_same_object_every_time()
    {
        var (_, vm) = Open(1, 2);

        Assert.True(ReferenceEquals(vm.Items, vm.Items),
            "Items hands back a different collection each time it is read. A binding keeps the instance " +
            "it was given, so the window would go on showing the first one forever.");
    }

    [Fact]
    public void Something_added_to_the_store_appears_in_the_list()
    {
        var (store, vm) = Open(1, 2);

        store.Add(Wiring.CreateSample(3));

        Assert.True(Shown(vm).Count == 3,
            $"Three items are in the store and the list shows {Shown(vm).Count}. This is what Lab 06's event " +
            "was for: the view model subscribes, and the window updates without anyone telling it to.");
    }

    [Fact]
    public void And_the_list_says_so()
    {
        var (store, vm) = Open(1);
        using var watch = new Watched(vm.Items);

        store.Add(Wiring.CreateSample(2));

        var announced = watch.Actions.Count == 0 ? "nothing" : string.Join(", ", watch.Actions);
        Assert.True(watch.Actions is [NotifyCollectionChangedAction.Add],
            $"One item arrived and the collection announced {announced}. It should be exactly one Add. " +
            "Rebuilding the list with Clear() and a loop announces a Reset first, and a Reset tells the window " +
            "to throw away every row and draw them all again — the flicker, the lost scroll position and the " +
            "lost selection all come from that one shortcut.");
    }

    [Fact]
    public void Adding_does_not_replace_the_collection()
    {
        var (store, vm) = Open(1);
        var before = vm.Items;

        store.Add(Wiring.CreateSample(2));

        Assert.True(ReferenceEquals(before, vm.Items),
            "The collection object changed when an item was added. The window is still bound to the old one.");
    }

    [Fact]
    public void The_count_matches_what_is_showing()
    {
        var (_, vm) = Open(1, 2, 3, 4);

        Assert.Equal(Shown(vm).Count, vm.Count);
    }

    [Fact]
    public void The_count_announces_itself()
    {
        var (store, vm) = Open(1);
        using var seen = new Announced(vm);

        store.Add(Wiring.CreateSample(2));

        Assert.True(seen.Saw(nameof(ICatalogueViewModel.Count)),
            $"An item arrived and Count went up, but the view model announced {seen.What}. " +
            "A status bar bound to Count would still show the old number.");
    }

    [Fact]
    public void An_empty_store_is_an_empty_list()
    {
        var vm = Wiring.CreateViewModel(Wiring.CreateStore());

        Assert.Empty(Shown(vm));
        Assert.Equal(0, vm.Count);
    }

    [Fact]
    public void Two_windows_share_nothing()
    {
        var (first, firstVm) = Open(1, 2);
        var (_, secondVm) = Open(5);

        first.Add(Wiring.CreateSample(3));

        Assert.True(Shown(secondVm).Count == 1,
            $"Adding to one store changed another view model's list ({Shown(secondVm).Count} instead of 1). " +
            "Something is static that shouldn't be.");
        Assert.False(ReferenceEquals(firstVm.Items, secondVm.Items), "Two view models are sharing one collection.");
    }

    [Fact]
    public void The_list_keeps_store_order()
    {
        var (store, vm) = Open(9, 4, 7, 1);

        Assert.Equal(Ids(store.All()), Ids(Shown(vm)));
    }
}
