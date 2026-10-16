using Capstone.Core.Model;
using Capstone.Core.Search;
using Capstone.Core.Storage;

namespace Capstone.Tests;

public class ItemSearchTests
{
    private static Pokedex Sample()
    {
        var dex = new Pokedex();
        dex.Add(new Pokemon { Id = "pkmn-004", Species = "Charmander", PrimaryType = PokemonType.Fire, Nickname = "Ember" });
        dex.Add(new Pokemon { Id = "pkmn-007", Species = "Squirtle", PrimaryType = PokemonType.Water });
        return dex;
    }

    [Fact]
    public void A_pokemon_is_found_by_its_type_even_with_a_nickname()
    {
        var found = ItemSearch.Find(Sample(), "fire");

        Assert.Equal("pkmn-004", Assert.Single(found).Id);
    }

    [Fact]
    public void A_null_store_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => ItemSearch.Find(null!, "x"));
    }
}
