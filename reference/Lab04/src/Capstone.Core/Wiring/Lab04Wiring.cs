using Capstone.Core.Model;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab04Wiring : ILab04Wiring
{
    private readonly Lab02Wiring _lab02 = new();

    private static readonly Trainer Ash = new() { Name = "Ash" };

    public IItemStore CreateStore() => _lab02.CreateStore();

    public Type BaseType => typeof(Pokemon);

    public IReadOnlyList<Type> KindTypes => [typeof(WildPokemon), typeof(CaughtPokemon)];

    public IItem CreateOfKind(int kindIndex, int seed)
    {
        // Same species table as Lab 02, so an Id still reads the same way; the kind changes the prefix.
        var sample = (Pokemon)_lab02.CreateSample(seed);

        return kindIndex switch
        {
            0 => new WildPokemon
            {
                Id = $"wild-{seed:D3}",
                Species = sample.Species,
                PrimaryType = sample.PrimaryType,
                Level = sample.Level,
                Habitat = "route 1",
            },
            1 => new CaughtPokemon
            {
                Id = $"caught-{seed:D3}",
                Species = sample.Species,
                PrimaryType = sample.PrimaryType,
                Level = sample.Level,
                Keeper = Ash,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kindIndex), kindIndex, "This Pokédex has two kinds."),
        };
    }

    /// <summary>Through the base. Which override runs is the runtime's business, not ours.</summary>
    public string Describe(IItem item) => item is Pokemon p
        ? p.Describe()
        : throw new ArgumentException($"{item.GetType().Name} is not a Pokémon.", nameof(item));

    public IReadOnlyList<string> DescribeAll(IItemStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        var lines = new List<string>(store.Count);
        foreach (var item in store.All())   // one loop, no idea which kind it is holding
            lines.Add(Describe(item));
        return lines;
    }

    /// <summary>The outside world sorting a Pokémon into a box: a switch expression over type patterns.</summary>
    public string Classify(IItem item) => item switch
    {
        null => throw new ArgumentNullException(nameof(item)),
        CaughtPokemon => "caught",
        WildPokemon => "wild",
        Pokemon => "pokémon of some other kind",   // a kind added later still lands somewhere sensible
        _ => "not a Pokémon at all",
    };

    public bool TryFindByName(IItemStore store, string name, out IItem? found)
    {
        ArgumentNullException.ThrowIfNull(store);

        found = null;
        if (string.IsNullOrWhiteSpace(name)) return false;

        foreach (var item in store.All())
        {
            if (!string.Equals(item.DisplayName, name, StringComparison.Ordinal)) continue;
            found = item;
            return true;
        }

        return false;
    }

    public int AddAll(IItemStore store, params IItem[] items)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(items);

        var added = 0;
        foreach (var item in items)
        {
            store.Add(item);   // the store's own rules decide: null and duplicates throw
            added++;
        }
        return added;
    }
}
