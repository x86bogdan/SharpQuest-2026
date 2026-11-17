using System.Text.Json;
using Capstone.Core.Json;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

/// <summary>
/// Session 6's live demos, ready to run on the projector (instructor; work/labs/06-events/RUN-OF-SHOW.md):
///
///   dotnet run --project src/Capstone.App -- --demo where     slide 7, Huddle B: one Where, two different rules, nothing changed inside
///   dotnet run --project src/Capstone.App -- --demo closure   slide 9: ask "what prints?" before you run it
///   dotnet run --project src/Capstone.App -- --demo crash     slide 13: fine as it is; delete the ? in Pokedex.Add, run again, put it back
///   dotnet run --project src/Capstone.App -- --demo unsub     slide 14: the unsubscribe, handed back as a function
///
/// Each demo loads its own Pokédex with NOBODY subscribed. The normal run attaches two subscribers before it
/// loads anything, so deleting the ? there never crashes: that's why the crash needs its own mode.
/// The code below is the slides' code, so what runs is what the room saw.
/// </summary>
static class Demos
{
    public static void Run(string which, string dataPath)
    {
        var store = Load(dataPath);
        switch (which)
        {
            case "where":
                // Same method, two rules, nothing recompiled inside it.
                Show("Starts with P", Where(store, item => item.DisplayName.StartsWith('P')));
                Show("Has a nickname", Where(store, item => item.DisplayName.Contains(" the ")));
                break;

            case "closure":
                var total = 0;
                ForEach(store, item => total += item.DisplayName.Length);
                Console.WriteLine(total);   // not 0: the lambda changed the total out here, not a copy of it
                Console.WriteLine($"({string.Join(" + ", store.All().Select(i => $"{i.DisplayName.Length} {i.DisplayName}"))})");
                break;

            case "crash":
                var empty = new Pokedex();   // nobody subscribed: ItemAdded is null, not an empty list
                empty.Add(store.All()[0]);
                Console.WriteLine($"Added {empty.All()[0].DisplayName} with nobody listening. Fine, because of the ? in ItemAdded?.Invoke(item).");
                Console.WriteLine("Now delete that ? in Pokedex.Add and run this again.");
                break;

            case "unsub":
                var heard = 0;
                var unsubscribe = Subscribe(store, _ => heard++);
                var fresh = (Pokedex)store;
                fresh.Add(Sample("pkmn-133", "Eevee"));
                Console.WriteLine($"Subscribed, added one: heard {heard}");
                unsubscribe();                // a function made inside Subscribe, still holding store and handler
                fresh.Add(Sample("pkmn-134", "Vaporeon"));
                Console.WriteLine($"Unsubscribed, added one more: still heard {heard}");
                break;

            default:
                Console.WriteLine("Which demo? --demo where | closure | crash | unsub");
                Environment.ExitCode = 1;
                break;
        }
    }

    // Slide 7: your code is the loop, the caller brings the rule.
    static IReadOnlyList<IItem> Where(IItemStore store, Func<IItem, bool> keep)
    {
        var kept = new List<IItem>();
        foreach (var item in store.All())
            if (keep(item))          // the caller decides, not you
                kept.Add(item);
        return kept;
    }

    // Slide 9's ForEach: do this to each one.
    static void ForEach(IItemStore store, Action<IItem> action)
    {
        foreach (var item in store.All())
            action(item);
    }

    // Slide 14: the event is on the store class, so get it back first; the unsubscribe is a closure.
    static Action Subscribe(IItemStore store, Action<IItem> handler)
    {
        var mine = (Pokedex)store;   // on the slide: (YourStore)store
        mine.ItemAdded += handler;
        return () => mine.ItemAdded -= handler;   // still knows handler
    }

    static void Show(string rule, IReadOnlyList<IItem> kept) =>
        Console.WriteLine($"{rule}: {(kept.Count == 0 ? "none" : string.Join(", ", kept.Select(i => i.DisplayName)))}");

    static IItem Sample(string id, string species) =>
        new PokemonMapper().FromJson($$"""{ "id": "{{id}}", "species": "{{species}}", "primaryType": "Water", "level": 10 }""")
        ?? throw new InvalidOperationException("The sample didn't map.");

    static Pokedex Load(string path)
    {
        var store = new Pokedex();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var mapper = new PokemonMapper();
        foreach (var element in doc.RootElement.EnumerateArray())
            if (mapper.FromJson(element.GetRawText()) is { } pokemon)
                store.Add(pokemon);   // the invalid entry is skipped quietly here
        return store;
    }
}
