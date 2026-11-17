using SharpQuest.Contracts;

namespace Capstone.Tests;

// The SearchMatcher specification, one rule at a time (R1–R8 in contracts/.../Lab03/Lab03.cs).
public class SearchMatcherTests
{
    private readonly SearchMatcher _matcher = SearchMatcher.Create();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void R1_an_empty_query_matches_any_text(string query)
    {
        Assert.True(_matcher.Matches("Pikachu", query));
    }

    [Theory]
    [InlineData("PIKA")]
    [InlineData("pika")]
    [InlineData("pIkAcHu")]
    public void R2_case_is_ignored(string query)
    {
        Assert.True(_matcher.Matches("Pikachu", query));
    }

    [Theory]
    [InlineData("Café au lait", "cafe")]
    [InlineData("Cafe au lait", "café")]
    [InlineData("Șoseaua Kiseleff", "sosea")]
    public void R3_diacritics_are_ignored_both_ways(string text, string query)
    {
        Assert.True(_matcher.Matches(text, query));
    }

    [Fact]
    public void R4_every_word_must_match()
    {
        Assert.True(_matcher.Matches("Charmander the fire lizard", "fire lizard"));
        Assert.False(_matcher.Matches("Charmander the fire lizard", "fire water"));
    }

    [Fact]
    public void R5_word_order_does_not_matter()
    {
        Assert.True(_matcher.Matches("Charmander the fire lizard", "lizard fire"));
    }

    [Fact]
    public void R6_a_word_can_match_inside_a_longer_word()
    {
        Assert.True(_matcher.Matches("Pikachu", "chu"));
    }

    [Fact]
    public void R7_a_null_text_never_matches()
    {
        Assert.False(_matcher.Matches(null, "anything"));
        Assert.False(_matcher.Matches(null, ""));
    }

    [Fact]
    public void R8_a_null_query_is_an_error()
    {
        Assert.Throws<ArgumentNullException>(() => _matcher.Matches("Pikachu", null!));
    }

    [Fact]
    public void Text_that_lacks_the_word_does_not_match()
    {
        Assert.False(_matcher.Matches("Bulbasaur", "pika"));
    }
}
