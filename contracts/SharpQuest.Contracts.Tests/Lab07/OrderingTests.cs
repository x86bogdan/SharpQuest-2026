namespace SharpQuest.Contracts.Tests.Lab07;

/// <summary>Lab 07: ordering, and a page of it.</summary>
public class OrderingTests : Lab07TestBase
{
    [Fact]
    public void Sorted_puts_the_names_in_order()
    {
        var items = Items(("c", "cherry"), ("a", "apple"), ("b", "banana"));

        Assert.Equal("a,b,c", Ids(Wiring.Sorted(items)));
    }

    [Fact]
    public void Sorted_ignores_case()
    {
        // Ordinal order would put Mango first: every capital letter sorts before every lower-case one.
        var items = Items(("1", "zebra"), ("2", "apple"), ("3", "Mango"));

        var actual = Ids(Wiring.Sorted(items));

        Assert.True(actual == "2,3,1",
            $"Sorted gave {actual}, expected 2,3,1 (apple, Mango, zebra). Compare names with " +
            "StringComparer.OrdinalIgnoreCase — plain ordinal order puts every capital letter before every lower-case one.");
    }

    [Fact]
    public void Sorted_breaks_a_tie_with_the_id()
    {
        var items = Items(("z", "Apple"), ("a", "apple"), ("m", "APPLE"));

        var actual = Ids(Wiring.Sorted(items));

        Assert.True(actual == "a,m,z",
            $"Three names that differ only in case came back as {actual}, expected a,m,z. " +
            "Equal names are a tie, and a tie is broken by Id — that's what ThenBy is for.");
    }

    [Fact]
    public void Sorted_keeps_every_item_and_invents_none()
    {
        var items = Mixed();

        var sorted = Wiring.Sorted(items);

        Assert.Equal(items.Count, sorted.Count);
        Assert.Equal(
            string.Join(",", items.Select(i => i.Id).OrderBy(id => id, StringComparer.Ordinal)),
            string.Join(",", sorted.Select(i => i.Id).OrderBy(id => id, StringComparer.Ordinal)));
    }

    [Fact]
    public void Sorted_does_not_reorder_the_sequence_it_was_given()
    {
        var items = Mixed();
        var before = Ids(items);

        _ = Wiring.Sorted(items);

        Assert.True(before == Ids(items),
            $"The list handed in came back as {Ids(items)} instead of {before}. " +
            "OrderBy returns a new sequence; List.Sort rearranges the caller's list in place.");
    }

    [Fact]
    public void Sorted_of_nothing_is_nothing()
    {
        Assert.Empty(Wiring.Sorted([]));
    }

    [Fact]
    public void Sorted_of_a_null_sequence_is_a_programming_error()
    {
        var ex = Record.Exception(() => Wiring.Sorted(null!));

        Assert.True(ex is not null, "Sorted(null) returned instead of throwing.");
        Assert.IsAssignableFrom<ArgumentNullException>(ex);
    }

    [Fact]
    public void Sorted_works_on_your_own_items_too()
    {
        var store = StoreWith(5, 1, 9, 3);

        Assert.Equal(Ids(InNameOrder(store.All())), Ids(Wiring.Sorted(store.All())));
    }

    [Fact]
    public void The_first_page_is_the_start_of_name_order()
    {
        var items = Mixed();

        Assert.Equal(Ids(InNameOrder(items).Take(2)), Ids(Wiring.Page(items, 0, 2)));
    }

    [Fact]
    public void The_next_page_carries_on_where_the_first_stopped()
    {
        var items = Mixed();

        var page = Ids(Wiring.Page(items, 2, 2));

        Assert.True(page == Ids(InNameOrder(items).Skip(2).Take(2)),
            $"Page(2, 2) gave {page}. Skip two of the ordered sequence, then take two — " +
            "skipping before ordering answers a different question.");
    }

    [Fact]
    public void A_page_past_the_end_is_empty()
    {
        Assert.Empty(Wiring.Page(Mixed(), 10, 3));
    }

    [Fact]
    public void A_last_page_gives_whatever_is_left()
    {
        var items = Mixed();

        Assert.Equal(Ids(InNameOrder(items).Skip(3)), Ids(Wiring.Page(items, 3, 10)));
    }

    [Fact]
    public void Taking_none_gives_none()
    {
        Assert.Empty(Wiring.Page(Mixed(), 1, 0));
    }

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(0, -5)]
    public void A_negative_page_is_a_programming_error(int skip, int take)
    {
        var ex = Record.Exception(() => Wiring.Page(Mixed(), skip, take));

        Assert.True(ex is not null,
            $"Page(items, {skip}, {take}) returned instead of throwing. Skip and Take shrug at negative numbers, " +
            "which is exactly why this one has to say what it means.");
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(ex);
    }

    [Fact]
    public void Page_of_a_null_sequence_is_a_programming_error()
    {
        var ex = Record.Exception(() => Wiring.Page(null!, 0, 1));

        Assert.True(ex is not null, "Page(null, 0, 1) returned instead of throwing.");
        Assert.IsAssignableFrom<ArgumentNullException>(ex);
    }
}
