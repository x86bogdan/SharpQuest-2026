namespace Capstone.Core.Model;

/// <summary>A Pokémon with a trainer. Setting the keeper also updates the trainer's last catch.</summary>
public sealed class CaughtPokemon : Pokemon
{
    // Backing fields: the properties below check a value, these store it.
    // (C# 14's `field` keyword would hide them; this course builds with C# 13, so they're written out.)
    private readonly Trainer? _keeper;

    public required Trainer Keeper
    {
        get => _keeper ?? throw new InvalidOperationException("Keeper was never set.");
        init
        {
            _keeper = value ?? throw new ArgumentNullException(nameof(Keeper), "A caught Pokémon has a trainer.");
            CaughtBy(_keeper);
        }
    }

    public override string Describe() => $"{Card()} · caught by {Keeper.Name}";
}
