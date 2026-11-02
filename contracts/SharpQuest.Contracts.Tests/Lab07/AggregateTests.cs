namespace SharpQuest.Contracts.Tests.Lab07;

/// <summary>Lab 07: four numbers about a whole sequence, and the empty case that catches everyone.</summary>
public class AggregateTests : Lab07TestBase
{
    [Fact]
    public void Summary_counts_the_items()
    {
        Assert.Equal(4, Wiring.Summary(Mixed()).Count);
    }

    [Fact]
    public void Summary_counts_the_different_initials()
    {
        // apple, Apple, banana, cherry pie → a, b, c
        var distinct = Wiring.Summary(Mixed()).DistinctInitials;

        Assert.True(distinct == 3,
            $"Four names beginning a, A, b, c gave {distinct} distinct initials, expected 3. " +
            "Upper-case and lower-case are the same letter — Distinct() compares what you give it.");
    }

    [Fact]
    public void Summary_finds_the_longest_name()
    {
        Assert.Equal("cherry pie", Wiring.Summary(Mixed()).LongestName);
    }

    [Fact]
    public void Summary_averages_the_name_lengths()
    {
        var items = Items(("a", "aa"), ("b", "bbbb"));

        var average = Wiring.Summary(items).AverageNameLength;

        Assert.True(Math.Abs(average - 3.0) < 0.0001,
            $"Two names of 2 and 4 characters averaged {average}, expected 3. " +
            "Average() over an int gives a double — don't let it round to an int on the way out.");
    }

    [Fact]
    public void Summary_of_nothing_answers_instead_of_throwing()
    {
        var thrown = Record.Exception(() => Wiring.Summary([]));

        Assert.True(thrown is null,
            $"Summary of an empty sequence threw {thrown?.GetType().Name}: {thrown?.Message}. " +
            "Average() and Max() throw on an empty sequence — which is exactly why nothing needs an answer of its own.");

        var stats = Wiring.Summary([]);

        Assert.True(stats is { Count: 0, DistinctInitials: 0, LongestName: null },
            $"An empty sequence summarised as {stats}, expected 0 / 0 / null / 0.");
        Assert.True(Math.Abs(stats.AverageNameLength) < 0.0001,
            $"An empty sequence averaged {stats.AverageNameLength}, expected 0. " +
            "Average() on an empty sequence throws InvalidOperationException, so this case needs an answer of its own.");
    }

    [Fact]
    public void Summary_of_a_null_sequence_is_a_programming_error()
    {
        var ex = Record.Exception(() => Wiring.Summary(null!));

        Assert.True(ex is not null, "Summary(null) returned instead of throwing.");
        Assert.IsAssignableFrom<ArgumentNullException>(ex);
    }

    [Fact]
    public void Summary_agrees_with_the_other_queries()
    {
        var store = StoreWith(1, 2, 3, 4, 5);
        var items = store.All();

        var stats = Wiring.Summary(items);

        Assert.Equal(items.Count, stats.Count);
        Assert.True(stats.LongestName == Wiring.Longest(items, 1).Single().DisplayName,
            $"Summary calls \"{stats.LongestName}\" the longest name, but Longest(items, 1) picks " +
            $"\"{Wiring.Longest(items, 1).Single().DisplayName}\". They answer the same question.");
    }
}
