namespace Capstone.Core.Model;

/// <summary>A Pokémon nobody owns yet.</summary>
public sealed class WildPokemon : Pokemon
{
    // Backing fields: the properties below check a value, these store it.
    // (C# 14's `field` keyword would hide them; this course builds with C# 13, so they're written out.)
    private readonly string _habitat = "the tall grass";

    /// <summary>Where it was seen. Never empty.</summary>
    public string Habitat
    {
        get => _habitat;
        init => _habitat = string.IsNullOrWhiteSpace(value) ? "the tall grass" : value.Trim();
    }

    public override string Describe() => $"{Card()} · wild, in {Habitat}";
}
