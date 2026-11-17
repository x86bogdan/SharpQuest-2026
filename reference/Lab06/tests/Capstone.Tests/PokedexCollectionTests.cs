using Capstone.Core.Collections;
using Capstone.Core.Model;
using Capstone.Core.Storage;

namespace Capstone.Tests;

// Lab 05: our own tests over the collection and the generic type.
public class PokedexCollectionTests
{
    private static Pokedex Dex()
    {
        var dex = new Pokedex();
        dex.Add(new WildPokemon { Id = "pkmn-004", Species = "Charmander", PrimaryType = PokemonType.Fire });
        dex.Add(new WildPokemon { Id = "pkmn-007", Species = "Squirtle", PrimaryType = PokemonType.Water });
        return dex;
    }

    [Fact]
    public void The_position_indexer_keeps_insertion_order()
    {
        Assert.Equal("pkmn-007", Dex()[1].Id);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void A_position_off_the_end_is_rejected(int index)
    {
        var dex = Dex();

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = dex[index]);
    }

    [Fact]
    public void The_id_indexer_is_case_sensitive_like_FindById()
    {
        var dex = Dex();

        Assert.NotNull(dex["pkmn-004"]);
        Assert.Null(dex["PKMN-004"]);
    }

    [Fact]
    public void Matching_is_lazy_enough_to_see_a_later_addition()
    {
        var dex = Dex();
        var matches = dex.Matching("");

        dex.Add(new WildPokemon { Id = "pkmn-025", Species = "Pikachu", PrimaryType = PokemonType.Electric });

        Assert.Equal(3, matches.Count());
    }

    [Fact]
    public void Matching_ignores_case_and_keeps_order()
    {
        Assert.Equal("pkmn-007", Assert.Single(Dex().Matching("SQUIRT")).Id);
    }
}

public class RecentlyUsedTests
{
    [Fact]
    public void The_newest_push_is_first()
    {
        var recent = new RecentlyUsed<string>(3);

        recent.Push("a");
        recent.Push("b");

        Assert.Equal(["b", "a"], recent.Items());
    }

    [Fact]
    public void A_repeat_moves_to_the_front_without_growing()
    {
        var recent = new RecentlyUsed<string>(3);

        foreach (var value in new[] { "a", "b", "a" }) recent.Push(value);

        Assert.Equal(["a", "b"], recent.Items());
        Assert.Equal(2, recent.Count);
    }

    [Fact]
    public void The_oldest_falls_off_the_end()
    {
        var recent = new RecentlyUsed<int>(2);

        foreach (var value in new[] { 1, 2, 3 }) recent.Push(value);

        Assert.Equal([3, 2], recent.Items());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_capacity_below_one_is_rejected(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecentlyUsed<string>(capacity));
    }

    [Fact]
    public void A_comparer_of_your_own_decides_what_counts_as_the_same()
    {
        var recent = new RecentlyUsed<string>(3, StringComparer.OrdinalIgnoreCase);

        recent.Push("Pikachu");
        recent.Push("PIKACHU");

        Assert.Equal(["PIKACHU"], recent.Items());
    }
}
