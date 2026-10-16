using Capstone.Core.Json;
using Capstone.Core.Model;
using Capstone.Core.Storage;

namespace Capstone.Tests;

// Tests over our own domain rules: the things the Lab 02 contract never checked.
public class PokemonTests
{
    private static Pokemon Pikachu() => new() { Id = "pkmn-025", Species = "Pikachu", PrimaryType = PokemonType.Electric };

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(101)]
    public void Level_outside_1_to_100_is_rejected(int level)
    {
        var pikachu = Pikachu();

        Assert.Throws<ArgumentOutOfRangeException>(() => pikachu.Level = level);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Level_accepts_the_boundaries(int level)
    {
        var pikachu = Pikachu();

        pikachu.Level = level;

        Assert.Equal(level, pikachu.Level);
    }

    [Fact]
    public void A_blank_species_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Pokemon { Id = "x", Species = "  ", PrimaryType = PokemonType.Fire });
    }

    [Fact]
    public void A_blank_nickname_is_stored_as_no_nickname()
    {
        var pikachu = Pikachu();

        pikachu.Nickname = "   ";

        Assert.Null(pikachu.Nickname);
        Assert.Equal("Pikachu", pikachu.DisplayName);
    }

    [Fact]
    public void The_display_name_includes_the_nickname()
    {
        var pikachu = Pikachu();

        pikachu.Nickname = " Sparky ";

        Assert.Equal("Sparky the Pikachu", pikachu.DisplayName);
    }

    [Fact]
    public void Describe_shows_both_types_when_there_are_two()
    {
        var charizard = new Pokemon { Id = "pkmn-006", Species = "Charizard", PrimaryType = PokemonType.Fire, SecondaryType = PokemonType.Dragon };

        Assert.Contains("Fire/Dragon", charizard.Describe());
    }

    [Fact]
    public void Catching_remembers_the_trainer_and_the_trainer_remembers_it()
    {
        var ash = new Trainer { Name = "Ash" };
        var pikachu = Pikachu();

        pikachu.CaughtBy(ash);

        Assert.Same(ash, pikachu.Owner);
        Assert.Same(pikachu, ash.LastCaught);
    }

    [Fact]
    public void Catching_with_no_trainer_does_not_crash()
    {
        var pikachu = Pikachu();

        var ex = Record.Exception(() => pikachu.CaughtBy(null));

        Assert.Null(ex);
        Assert.Null(pikachu.Owner);
    }

    [Fact]
    public void A_dto_with_an_impossible_level_is_rejected()
    {
        var dto = new PokemonDto("pkmn-001", "Bulbasaur", PokemonType.Grass, null, 250, null);

        Assert.Null(PokemonMapper.FromDto(dto));
    }

    [Fact]
    public void The_pokedex_looks_ids_up_exactly()
    {
        var pokedex = new Pokedex();
        pokedex.Add(Pikachu());

        Assert.NotNull(pokedex.FindById("pkmn-025"));
        Assert.Null(pokedex.FindById("PKMN-025"));
    }
}
