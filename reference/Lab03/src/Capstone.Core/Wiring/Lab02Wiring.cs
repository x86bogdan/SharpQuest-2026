using Capstone.Core.Json;
using Capstone.Core.Model;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab02Wiring : ILab02Wiring
{
    private static readonly (string Species, PokemonType Type)[] Starters =
    [
        ("Bulbasaur", PokemonType.Grass),
        ("Charmander", PokemonType.Fire),
        ("Squirtle", PokemonType.Water),
        ("Pikachu", PokemonType.Electric),
    ];

    public IItemStore CreateStore() => new Pokedex();

    public IItemMapper CreateMapper() => new PokemonMapper();

    public IItem CreateSample(int seed)
    {
        var (species, type) = Starters[Math.Abs(seed) % Starters.Length];
        return new Pokemon
        {
            Id = $"pkmn-{seed:D3}",
            Species = species,
            PrimaryType = type,
            Level = 1 + Math.Abs(seed) % 100,
        };
    }
}
