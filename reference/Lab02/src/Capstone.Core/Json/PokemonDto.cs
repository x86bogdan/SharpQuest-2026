using Capstone.Core.Model;

namespace Capstone.Core.Json;

/// <summary>
/// The shape of a Pokémon on disk. Everything is nullable on purpose: JSON from outside
/// can be missing anything, and the mapper decides what is acceptable.
/// </summary>
public sealed record PokemonDto(
    string? Id,
    string? Species,
    PokemonType? PrimaryType,
    PokemonType? SecondaryType,
    int? Level,
    string? Nickname);
