using Capstone.Core.Model;
using Capstone.Core.Queries;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Tests;

/// <summary>My own tests for the queries I wrote — the extension method and the summary.</summary>
public class ItemQueriesTests
{
    private static Pokedex Dex()
    {
        var dex = new Pokedex();
        dex.Add(new WildPokemon { Id = "pkmn-001", Species = "Bulbasaur", PrimaryType = PokemonType.Grass });
        dex.Add(new WildPokemon { Id = "pkmn-004", Species = "Charmander", PrimaryType = PokemonType.Fire });
        dex.Add(new WildPokemon { Id = "pkmn-025", Species = "Pikachu", PrimaryType = PokemonType.Electric });
        dex.Add(new WildPokemon { Id = "pkmn-151", Species = "Mew", PrimaryType = PokemonType.Psychic });
        return dex;
    }

    [Fact]
    public void Longest_names_come_first()
    {
        Assert.Equal("Charmander", Dex().Longest(1).Single().DisplayName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(99)]
    public void Asking_for_n_gives_at_most_n(int n)
    {
        Assert.True(Dex().Longest(n).Count() <= n);
    }

    [Fact]
    public void A_negative_count_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Dex().Longest(-1).ToList());
    }

    [Fact]
    public void A_nickname_changes_the_answer_because_it_changes_the_display_name()
    {
        var dex = Dex();
        ((Pokemon)dex["pkmn-025"]!).Nickname = "Sparky McLightning";

        Assert.Equal("Sparky McLightning the Pikachu", dex.Longest(1).Single().DisplayName);
    }

    [Fact]
    public void One_line_lists_them_in_store_order()
    {
        Assert.Equal("Bulbasaur, Charmander, Pikachu, Mew", Dex().OneLine());
    }

    [Fact]
    public void An_empty_pokedex_says_so()
    {
        Assert.True(new Pokedex().IsEmpty());
        Assert.False(Dex().IsEmpty());
    }

    [Fact]
    public void The_summary_counts_initials_once_per_letter()
    {
        // Bulbasaur, Charmander, Pikachu, Mew → B, C, P, M
        Assert.Equal(4, new Capstone.Core.Wiring.Lab07Wiring().Summary(Dex().All()).DistinctInitials);
    }
}
