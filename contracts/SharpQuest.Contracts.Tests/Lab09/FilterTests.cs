namespace SharpQuest.Contracts.Tests.Lab09;

/// <summary>Lab 09: the search box. One property, and the list follows it.</summary>
public class FilterTests : Lab09TestBase
{
    /// <summary>A word from the first item's display name, long enough not to match by accident.</summary>
    private static string WordFrom(IItem item) =>
        item.DisplayName.Split(' ').OrderByDescending(w => w.Length).First();

    [Fact]
    public void The_filter_narrows_the_list()
    {
        var (store, vm) = Open(1, 2, 3, 4);
        var wanted = store.All()[0];

        vm.Filter = WordFrom(wanted);

        Assert.Contains(wanted.Id, Shown(vm).Select(i => i.Id));
        Assert.True(Shown(vm).All(i => i.DisplayName.Contains(WordFrom(wanted), StringComparison.OrdinalIgnoreCase)),
            $"With the filter set to \"{WordFrom(wanted)}\", the list still shows {Ids(Shown(vm))}.");
    }

    [Fact]
    public void The_filter_ignores_case()
    {
        var (store, vm) = Open(1, 2, 3);
        var wanted = store.All()[0];

        vm.Filter = WordFrom(wanted).ToUpperInvariant();

        Assert.Contains(wanted.Id, Shown(vm).Select(i => i.Id));
    }

    [Fact]
    public void A_blank_filter_shows_everything()
    {
        var (store, vm) = Open(1, 2, 3);

        vm.Filter = "";

        Assert.Equal(Ids(store.All()), Ids(Shown(vm)));
    }

    [Fact]
    public void A_filter_that_matches_nothing_shows_nothing()
    {
        var (_, vm) = Open(1, 2, 3);

        vm.Filter = "zzqx-no-such-thing";

        Assert.Empty(Shown(vm));
        Assert.True(vm.Count == 0, $"The list is empty but Count says {vm.Count}.");
    }

    [Fact]
    public void Clearing_the_filter_brings_everything_back()
    {
        var (store, vm) = Open(1, 2, 3);
        vm.Filter = "zzqx-no-such-thing";

        vm.Filter = "";

        Assert.Equal(Ids(store.All()), Ids(Shown(vm)));
    }

    [Fact]
    public void Filtering_does_not_touch_the_store()
    {
        var (store, vm) = Open(1, 2, 3);

        vm.Filter = "zzqx-no-such-thing";

        Assert.True(store.Count == 3,
            $"Filtering left {store.Count} items in the store. The filter decides what is *shown*, " +
            "not what exists — removing from the store to hide a row is how a search box deletes someone's data.");
    }

    [Fact]
    public void Filtering_does_not_replace_the_collection()
    {
        var (_, vm) = Open(1, 2, 3);
        var before = vm.Items;

        vm.Filter = "zzqx-no-such-thing";

        Assert.True(ReferenceEquals(before, vm.Items),
            "Filtering swapped in a new collection. The window is bound to the old one and will not change.");
    }

    [Fact]
    public void A_null_filter_is_the_same_as_a_blank_one()
    {
        var (store, vm) = Open(1, 2);

        var ex = Record.Exception(() => vm.Filter = null!);

        Assert.True(ex is null, $"Setting Filter to null threw {ex?.GetType().Name}. A binding will hand you null " +
                                "the moment the user clears the box.");
        Assert.Equal(Ids(store.All()), Ids(Shown(vm)));
    }

    [Fact]
    public void Something_added_while_filtered_obeys_the_filter()
    {
        var (store, vm) = Open(1, 2);
        vm.Filter = "zzqx-no-such-thing";

        store.Add(Wiring.CreateSample(3));

        Assert.True(Shown(vm).Count == 0,
            $"The filter matches nothing, but a newly added item appeared anyway ({Ids(Shown(vm))}). " +
            "Whatever decides what the list shows has to apply to arrivals too.");
    }
}
