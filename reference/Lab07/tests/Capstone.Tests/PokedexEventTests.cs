using Capstone.Core.Model;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Tests;

// Lab 06: our own tests over the event and over functions we pass around.
public class PokedexEventTests
{
    private static WildPokemon Pikachu(string id = "pkmn-025") =>
        new() { Id = id, Species = "Pikachu", PrimaryType = PokemonType.Electric };

    [Fact]
    public void Adding_announces_the_entry_that_was_added()
    {
        var dex = new Pokedex();
        IItem? announced = null;
        dex.ItemAdded += item => announced = item;

        var pikachu = Pikachu();
        dex.Add(pikachu);

        Assert.Same(pikachu, announced);
    }

    [Fact]
    public void A_rejected_duplicate_announces_nothing()
    {
        var dex = new Pokedex();
        dex.Add(Pikachu());
        var announcements = 0;
        dex.ItemAdded += _ => announcements++;

        Assert.Throws<ArgumentException>(() => dex.Add(Pikachu()));
        Assert.Equal(0, announcements);
    }

    [Fact]
    public void Unsubscribing_stops_only_that_handler()
    {
        var dex = new Pokedex();
        var quiet = 0;
        var loud = 0;
        void Quiet(IItem item) => quiet++;
        dex.ItemAdded += Quiet;
        dex.ItemAdded += _ => loud++;

        dex.Add(Pikachu("pkmn-001"));
        dex.ItemAdded -= Quiet;
        dex.Add(Pikachu("pkmn-002"));

        Assert.Equal(1, quiet);
        Assert.Equal(2, loud);
    }

    [Fact]
    public void A_pokedex_nobody_listens_to_still_works()
    {
        var dex = new Pokedex();

        var ex = Record.Exception(() => dex.Add(Pikachu()));

        Assert.Null(ex);
        Assert.Equal(1, dex.Count);
    }

    [Fact]
    public void The_count_is_already_right_when_the_subscriber_runs()
    {
        var dex = new Pokedex();
        var countsSeen = new List<int>();
        dex.ItemAdded += _ => countsSeen.Add(dex.Count);

        dex.Add(Pikachu("pkmn-001"));
        dex.Add(Pikachu("pkmn-002"));

        Assert.Equal("1,2", string.Join(",", countsSeen));
    }

    [Fact]
    public void A_rule_passed_in_decides_what_comes_back()
    {
        var dex = new Pokedex();
        dex.Add(Pikachu("pkmn-025"));
        dex.Add(new WildPokemon { Id = "pkmn-004", Species = "Charmander", PrimaryType = PokemonType.Fire });

        Func<IItem, bool> fireOnly = item => item is Pokemon { PrimaryType: PokemonType.Fire };
        var wiring = new Capstone.Core.Wiring.Lab06Wiring();

        Assert.Equal("pkmn-004", Assert.Single(wiring.Where(dex, fireOnly)).Id);
    }
}
