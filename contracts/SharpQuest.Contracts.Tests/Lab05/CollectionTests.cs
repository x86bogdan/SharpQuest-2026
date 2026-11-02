namespace SharpQuest.Contracts.Tests.Lab05;

/// <summary>Lab 05: the store is a collection now — foreach over it, index into it, iterate it lazily.</summary>
public class CollectionTests : Lab05TestBase
{
    [Fact]
    public void Wiring_class_can_be_created()
    {
        Assert.NotNull(Wiring);
    }

    [Fact]
    public void The_store_is_still_an_item_store()
    {
        Assert.True(typeof(IItemStore).IsAssignableFrom(CollectionType),
            $"{Name(CollectionType)} no longer implements IItemStore. Lab 02 still has to work.");
    }

    [Fact]
    public void You_can_foreach_over_the_store_itself()
    {
        Assert.True(typeof(IEnumerable<IItem>).IsAssignableFrom(CollectionType),
            $"{Name(CollectionType)} does not implement IEnumerable<IItem>, so 'foreach (var item in store)' won't compile. " +
            "Implement it — your IItem can be more specific than IItem only if it still satisfies IEnumerable<IItem>.");
    }

    [Fact]
    public void Something_in_the_collection_is_written_with_yield()
    {
        Assert.True(HasIteratorMember(CollectionType),
            $"{Name(CollectionType)} contains no method written with 'yield return'. " +
            "Handing back your list's enumerator is not writing an iterator: write GetEnumerator (or Matching) with yield.");
    }

    [Fact]
    public void Your_collection_does_not_lean_on_the_catch_up_kit()
    {
        foreach (var type in new[] { Wiring.GetType(), CollectionType })
        {
            var borrowed = KitReach.Student.FirstKitMemberReachedBy(type);
            Assert.True(borrowed is null,
                $"{Name(type)} reaches {borrowed}, which comes from the catch-up kit. The kit unblocks later labs; " +
                "it doesn't do this one. Write the enumerator and the indexers on your own store, and stop using the kit here.");
        }
    }

    [Fact]
    public void There_is_an_indexer_by_position_and_one_by_id()
    {
        Assert.True(Indexer(CollectionType, typeof(int)) is not null,
            $"{Name(CollectionType)} has no 'this[int]' indexer.");
        Assert.True(Indexer(CollectionType, typeof(string)) is not null,
            $"{Name(CollectionType)} has no 'this[string]' indexer.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void The_position_indexer_follows_the_order_items_were_added(int index)
    {
        var store = StoreWith(7, 3, 9);

        var expected = store.All()[index];
        var actual = Wiring.At(store, index);

        Assert.True(expected.Id == actual.Id,
            $"store[{index}] gave \"{actual.DisplayName}\", but the item added at position {index} was \"{expected.DisplayName}\". " +
            "The position indexer counts from the first item added.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void A_position_outside_the_collection_is_rejected(int index)
    {
        var store = StoreWith(1, 2, 3);

        var ex = Record.Exception(() => Wiring.At(store, index));

        Assert.True(ex is not null, $"store[{index}] on a store of 3 returned instead of throwing.");
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(ex);
    }

    [Fact]
    public void The_id_indexer_finds_what_FindById_finds()
    {
        var store = StoreWith(4, 5);
        var wanted = store.All()[1];

        Assert.Equal(wanted.Id, Wiring.ById(store, wanted.Id)?.Id);
    }

    [Fact]
    public void The_id_indexer_gives_null_for_an_id_nobody_has()
    {
        var store = StoreWith(4, 5);

        Assert.Null(Wiring.ById(store, "no-such-id"));
    }

    [Fact]
    public void Foreach_over_the_store_sees_every_item_in_order()
    {
        var store = StoreWith(5, 1, 9, 3);

        Assert.Equal(store.All().Select(i => i.Id), Wiring.EachInOrder(store).Select(i => i.Id));
    }

    [Fact]
    public void Foreach_over_an_empty_store_sees_nothing()
    {
        Assert.Empty(Wiring.EachInOrder(Wiring.CreateStore()));
    }

    [Fact]
    public void Enumerating_twice_gives_the_same_answer()
    {
        var store = StoreWith(2, 8);

        Assert.Equal(Wiring.EachInOrder(store).Select(i => i.Id), Wiring.EachInOrder(store).Select(i => i.Id));
        Assert.Equal(2, store.Count);
    }

    [Fact]
    public void Adding_after_you_enumerated_is_visible_next_time()
    {
        var store = StoreWith(1);

        var before = Wiring.EachInOrder(store).Count;
        store.Add(Wiring.CreateSample(2));

        Assert.Equal(1, before);
        Assert.Equal(2, Wiring.EachInOrder(store).Count);
    }
}
