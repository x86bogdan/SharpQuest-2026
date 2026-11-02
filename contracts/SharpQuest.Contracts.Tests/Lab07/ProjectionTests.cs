namespace SharpQuest.Contracts.Tests.Lab07;

/// <summary>Lab 07: projection, and the thing that surprises everyone — a query is not a result.</summary>
public class ProjectionTests : Lab07TestBase
{
    [Fact]
    public void Names_gives_one_name_per_item()
    {
        var store = StoreWith(1, 2, 3, 4);

        Assert.Equal(4, Wiring.Names(store).Count());
    }

    [Fact]
    public void Names_gives_the_display_names_in_store_order()
    {
        var store = StoreWith(7, 3, 9);

        var expected = string.Join(",", store.All().Select(i => i.DisplayName));
        var actual = string.Join(",", Wiring.Names(store));

        Assert.True(expected == actual,
            $"Names gave \"{actual}\" but the store holds \"{expected}\". One name per item, in store order.");
    }

    [Fact]
    public void Names_of_an_empty_store_is_empty()
    {
        Assert.Empty(Wiring.Names(Wiring.CreateStore()));
    }

    [Fact]
    public void Names_is_a_query_not_a_result()
    {
        var store = StoreWith(1, 2, 3);

        var names = Wiring.Names(store);          // asked for…
        store.Add(Wiring.CreateSample(4));        // …and only now is there a fourth item

        var counted = names.Count();

        Assert.True(counted == 4,
            $"After a fourth item was added, Names counted {counted}. A LINQ query runs when you enumerate it, " +
            "not when you write it — so query the store itself (your Lab 05 collection), not the snapshot All() hands you.");
    }

    [Fact]
    public void Names_notices_a_removal_too()
    {
        var store = StoreWith(1, 2, 3);
        var doomed = store.All()[1].Id;

        var names = Wiring.Names(store);
        store.Remove(doomed);

        Assert.Equal(2, names.Count());
    }

    [Fact]
    public void Names_can_be_enumerated_twice()
    {
        var store = StoreWith(2, 4, 6);

        var names = Wiring.Names(store);
        var first = string.Join(",", names);
        var second = string.Join(",", names);

        Assert.True(first == second,
            $"Enumerating the same query twice gave \"{first}\" then \"{second}\". " +
            "A query you can only read once usually means an enumerator was handed out instead of a query.");
    }

    [Fact]
    public void Names_does_not_change_the_store()
    {
        var store = StoreWith(1, 2, 3);

        _ = Wiring.Names(store).ToList();

        Assert.Equal(3, store.Count);
    }
}
