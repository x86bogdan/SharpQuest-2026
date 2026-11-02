namespace Capstone.Core.Model;

/// <summary>A trainer isn't a Pokémon and never goes in the Pokédex, but it can describe itself too.</summary>
public sealed class Trainer : IDescribable
{
    public required string Name { get; init; }

    public Pokemon? LastCaught { get; set; }

    public string Describe() => $"Trainer {Name} · last caught {LastCaught?.DisplayName ?? "nothing"}";
}
