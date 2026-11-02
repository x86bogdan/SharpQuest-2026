using SharpQuest.Contracts;

namespace Capstone.Core.Model;

/// <summary>
/// Everything every Pokémon has. Abstract on purpose: a Pokémon is always of some kind
/// (wild or caught), never "just a Pokémon". Lab 04 made it abstract without touching
/// anything Lab 02 and Lab 03 test — the store, the mapper and search all speak IItem.
/// </summary>
public abstract class Pokemon : IItem, IDescribable
{
    // Backing fields: the properties below check a value, these store it.
    // (C# 14's `field` keyword would hide them; this course builds with C# 13, so they're written out.)
    private readonly string _id = "";
    private readonly string _species = "";
    private int _level = 1;
    private string? _nickname;

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A Pokémon needs an Id.", nameof(Id))
            : value;
    }

    public required string Species
    {
        get => _species;
        init => _species = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A Pokémon needs a species.", nameof(Species))
            : value.Trim();
    }

    public required PokemonType PrimaryType { get; init; }

    /// <summary>Most Pokémon have one type. Null means "no second type", not "unknown".</summary>
    public PokemonType? SecondaryType { get; init; }

    public int Level
    {
        get => _level;
        set => _level = value is >= 1 and <= 100
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Level), value, "Level is between 1 and 100.");
    }

    /// <summary>Blank nicknames are stored as null, so there is only one way to say "no nickname".</summary>
    public string? Nickname
    {
        get => _nickname;
        set => _nickname = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public Trainer? Owner { get; private set; }

    public string DisplayName => Nickname is null ? Species : $"{Nickname} the {Species}";

    public void CaughtBy(Trainer? trainer)
    {
        Owner = trainer;
        // Nothing to record if nobody caught it.
        if (trainer is not null) trainer.LastCaught = this;
    }

    /// <summary>Each kind says this differently. That's the whole point of the abstract base.</summary>
    public abstract string Describe();

    /// <summary>The part of the description every kind shares.</summary>
    protected string Card()
    {
        var types = SecondaryType is { } second ? $"{PrimaryType}/{second}" : $"{PrimaryType}";
        return $"#{Id} {DisplayName} · {types} · Lv {Level}";
    }

    public override string ToString() => Describe();
}
