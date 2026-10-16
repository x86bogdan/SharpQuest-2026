using SharpQuest.Contracts;

namespace Capstone.Core.Model;

public sealed class Pokemon : IItem, IDescribable
{
    // Validation in the setter needs a backing field: the property checks, the field stores.
    // (C# 14 on .NET 10 can write this without the private field, using the `field` keyword.
    //  This course builds with C# 13 so the same code compiles on the lab PCs' .NET 9.)
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

    public string Describe()
    {
        var types = SecondaryType is { } second ? $"{PrimaryType}/{second}" : $"{PrimaryType}";
        return $"#{Id} {DisplayName} · {types} · Lv {Level}";
    }

    public override string ToString() => Describe();
}
