namespace SharpQuest.Contracts.Tests.Lab08;

/// <summary>
/// Lab 08: what goes in comes back. Not "it didn't throw" — the same things, with the same values.
/// </summary>
public class RoundTripTests : Lab08TestBase
{
    [Fact]
    public async Task Everything_saved_comes_back()
    {
        var store = StoreWith(1, 2, 3, 4);

        var loaded = await SaveAndLoad(store);

        Assert.True(loaded.Count == 4,
            $"Four items went in and {loaded.Count} came back.");
    }

    [Fact]
    public async Task The_order_is_the_order_it_was_saved_in()
    {
        var store = StoreWith(7, 3, 9, 1);

        var loaded = await SaveAndLoad(store);

        Assert.True(Ids(store.All()) == Ids(loaded.All()),
            $"Saved {Ids(store.All())} and loaded {Ids(loaded.All())}. A store keeps the order things were added in, " +
            "and so should the file.");
    }

    [Fact]
    public async Task Every_value_survives()
    {
        var store = StoreWith(1, 2, 3);

        var loaded = await SaveAndLoad(store);

        foreach (var saved in store.All())
        {
            var back = loaded.FindById(saved.Id);
            Assert.True(back is not null, $"'{saved.DisplayName}' was saved and did not come back at all.");
            AssertSameItem(saved, back!);
        }
    }

    [Fact]
    public async Task An_empty_store_round_trips_to_an_empty_store()
    {
        var loaded = await SaveAndLoad(Wiring.CreateStore());

        Assert.Equal(0, loaded.Count);
    }

    [Fact]
    public async Task A_store_of_one_round_trips()
    {
        var store = StoreWith(5);

        var loaded = await SaveAndLoad(store);

        Assert.Equal(1, loaded.Count);
        AssertSameItem(store.All()[0], loaded.All()[0]);
    }

    [Fact]
    public async Task Saving_again_replaces_what_was_there()
    {
        var path = NewPath();
        await Wiring.SaveAsync(StoreWith(1, 2, 3), path);

        await Wiring.SaveAsync(StoreWith(8, 9), path);
        var loaded = await Wiring.LoadAsync(path);

        Assert.True(loaded.Count == 2,
            $"Three items were saved, then two, and {loaded.Count} came back. A save replaces the file — " +
            "opening it in append mode adds a second document to the end of the first.");
    }

    [Fact]
    public async Task The_file_is_a_snapshot_not_a_live_view()
    {
        var store = StoreWith(1, 2);
        var path = NewPath();

        await Wiring.SaveAsync(store, path);
        store.Add(Wiring.CreateSample(3));          // after the save
        var loaded = await Wiring.LoadAsync(path);

        Assert.True(loaded.Count == 2,
            $"Two items were saved, a third was added afterwards, and the file gave back {loaded.Count}. " +
            "Saving writes what the store held at that moment.");
    }

    [Fact]
    public async Task What_comes_back_is_a_working_store()
    {
        var store = StoreWith(1, 2);

        var loaded = await SaveAndLoad(store);
        var first = loaded.All()[0];

        Assert.True(loaded.FindById(first.Id) is not null, "FindById can't find an item the loaded store holds.");
        var again = Record.Exception(() => loaded.Add(first));
        Assert.True(again is not null,
            "The loaded store accepted an item whose Id it already holds. It's a real store — Lab 02's rules still apply to it.");
    }

    [Fact]
    public async Task Loading_twice_gives_two_separate_stores()
    {
        var path = NewPath();
        await Wiring.SaveAsync(StoreWith(1, 2), path);

        var first = await Wiring.LoadAsync(path);
        var second = await Wiring.LoadAsync(path);
        first.Add(Wiring.CreateSample(3));

        Assert.True(second.Count == 2,
            $"Adding to one loaded store changed another ({second.Count} instead of 2). Each load builds its own.");
    }

    [Fact]
    public async Task A_file_written_before_the_restart_still_reads_afterwards()
    {
        // The required feature, as literally as a test can put it: nothing of the first run survives
        // except the file itself.
        var path = NewPath();
        await Wiring.SaveAsync(StoreWith(2, 4, 6), path);

        var afterRestart = (ILab08Wiring)Activator.CreateInstance(Wiring.GetType())!;
        var loaded = await afterRestart.LoadAsync(path);

        Assert.True(loaded.Count == 3,
            $"A fresh instance read {loaded.Count} of the 3 saved items. Everything needed to read the file has to be " +
            "in the file — a store that remembers its own contents in a static field passes every other test and this one fails.");
    }

    [Fact]
    public async Task A_big_store_round_trips()
    {
        var store = StoreWith(Enumerable.Range(1, 50).ToArray());

        var loaded = await SaveAndLoad(store);

        Assert.Equal(50, loaded.Count);
        Assert.Equal(Ids(store.All()), Ids(loaded.All()));
    }

    [Fact]
    public async Task Saving_an_empty_store_over_a_full_file_empties_it()
    {
        var path = NewPath();
        await Wiring.SaveAsync(StoreWith(1, 2, 3), path);

        await Wiring.SaveAsync(Wiring.CreateStore(), path);
        var loaded = await Wiring.LoadAsync(path);

        Assert.True(loaded.Count == 0,
            $"Everything was deleted and saved, and {loaded.Count} items came back. " +
            "An empty store is something to write down, not a reason to skip writing.");
    }

    [Fact]
    public async Task Saving_does_not_change_the_store()
    {
        var store = StoreWith(1, 2, 3);

        await Wiring.SaveAsync(store, NewPath());

        Assert.Equal(3, store.Count);
    }

    [Fact]
    public async Task The_saved_file_has_something_in_it()
    {
        var path = NewPath();

        await Wiring.SaveAsync(StoreWith(1, 2, 3), path);

        var text = await File.ReadAllTextAsync(path);
        Assert.True(text.Trim().Length > 0, "The file exists but is empty.");
    }
}
