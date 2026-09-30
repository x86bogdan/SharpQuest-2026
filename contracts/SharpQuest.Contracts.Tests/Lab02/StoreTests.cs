using SharpQuest.Contracts.Tests.Support;

namespace SharpQuest.Contracts.Tests.Lab02;

/// <summary>Lab 02: the behaviour promised by <see cref="IItemStore"/> and <see cref="ILab02Wiring.CreateSample"/>.</summary>
public class StoreTests
{
    // Resolved inside each test (not in the constructor) so a missing wiring class skips the test.
    private ILab02Wiring? _wiringCache;
    private ILab02Wiring Wiring => _wiringCache ??= StudentCode.Wiring<ILab02Wiring>();

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(42)]
    public void Sample_items_have_an_id_and_a_display_name(int seed)
    {
        var item = Wiring.CreateSample(seed);

        Assert.False(string.IsNullOrWhiteSpace(item.Id), "Id is empty.");
        Assert.False(string.IsNullOrWhiteSpace(item.DisplayName), "DisplayName is empty.");
    }

    [Fact]
    public void Same_seed_gives_the_same_id()
    {
        Assert.Equal(Wiring.CreateSample(7).Id, Wiring.CreateSample(7).Id);
    }

    [Fact]
    public void Different_seeds_give_different_ids()
    {
        var ids = Enumerable.Range(1, 100).Select(seed => Wiring.CreateSample(seed).Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void A_new_store_is_empty()
    {
        var store = Wiring.CreateStore();

        Assert.Equal(0, store.Count);
        Assert.Empty(store.All());
    }

    [Fact]
    public void Added_item_can_be_found_by_id()
    {
        var store = Wiring.CreateStore();
        var item = Wiring.CreateSample(1);

        store.Add(item);

        var found = store.FindById(item.Id);
        Assert.NotNull(found);
        Assert.Equal(item.Id, found.Id);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Unknown_id_finds_nothing()
    {
        var store = Wiring.CreateStore();
        store.Add(Wiring.CreateSample(1));

        Assert.Null(store.FindById("definitely-not-an-id"));
    }

    [Fact]
    public void Ids_are_case_sensitive()
    {
        var store = Wiring.CreateStore();
        var item = Wiring.CreateSample(1);
        store.Add(item);

        var shouted = item.Id.ToUpperInvariant();
        if (shouted == item.Id) shouted = item.Id.ToLowerInvariant();
        if (shouted == item.Id) return; // an Id with no letters can't be tested this way

        Assert.True(store.FindById(shouted) is null,
            $"FindById(\"{shouted}\") found the item stored as \"{item.Id}\". Ids are case-sensitive: use StringComparer.Ordinal.");
    }

    [Fact]
    public void Adding_null_throws_ArgumentNullException()
    {
        var store = Wiring.CreateStore();

        Assert.Throws<ArgumentNullException>(() => store.Add(null!));
    }

    [Fact]
    public void Adding_a_duplicate_id_throws_and_changes_nothing()
    {
        var store = Wiring.CreateStore();
        store.Add(Wiring.CreateSample(1));

        var ex = Record.Exception(() => store.Add(Wiring.CreateSample(1)));

        Assert.True(ex is not null, "Adding a second item with the same Id should throw ArgumentException, but nothing was thrown.");
        Assert.IsAssignableFrom<ArgumentException>(ex);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void All_keeps_insertion_order()
    {
        var store = Wiring.CreateStore();
        int[] seeds = [5, 1, 9, 3];
        foreach (var seed in seeds) store.Add(Wiring.CreateSample(seed));

        var expected = seeds.Select(s => Wiring.CreateSample(s).Id);
        var actual = store.All().Select(i => i.Id);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void All_returns_a_snapshot()
    {
        var store = Wiring.CreateStore();
        store.Add(Wiring.CreateSample(1));

        var before = store.All();
        store.Add(Wiring.CreateSample(2));
        store.Remove(Wiring.CreateSample(1).Id);

        Assert.True(before.Count == 1 && before[0].Id == Wiring.CreateSample(1).Id,
            "A list returned by All() changed after the store changed. Return a copy, not your internal list.");
    }

    [Fact]
    public void Remove_deletes_the_item_and_reports_it()
    {
        var store = Wiring.CreateStore();
        var item = Wiring.CreateSample(1);
        store.Add(item);
        store.Add(Wiring.CreateSample(2));

        Assert.True(store.Remove(item.Id));
        Assert.Null(store.FindById(item.Id));
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Removing_an_unknown_id_returns_false()
    {
        var store = Wiring.CreateStore();
        store.Add(Wiring.CreateSample(1));

        Assert.False(store.Remove("definitely-not-an-id"));
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void A_removed_id_can_be_added_again()
    {
        var store = Wiring.CreateStore();
        var item = Wiring.CreateSample(1);
        store.Add(item);
        store.Remove(item.Id);

        store.Add(Wiring.CreateSample(1));

        Assert.NotNull(store.FindById(item.Id));
    }

    [Fact]
    public void Two_stores_do_not_share_items()
    {
        var first = Wiring.CreateStore();
        var second = Wiring.CreateStore();

        first.Add(Wiring.CreateSample(1));

        Assert.True(second.Count == 0, "Adding to one store changed another. Is your storage static?");
    }
}
