using System.Text.Json;
using Capstone.Core.Collections;
using Capstone.Core.Json;
using Capstone.Core.Model;
using Capstone.Core.Search;
using Capstone.Core.Storage;
using Capstone.Core.Wiring;
using Capstone.Core.Web;

// Lab 02–03 reference: load the Pokédex from JSON, skip what's invalid, show what's left, search it.
// dotnet run --project src/Capstone.App                     → local file only
// dotnet run --project src/Capstone.App -- --search "fire"  → only the matches
// dotnet run --project src/Capstone.App -- --kinds          → what each one is, sorted by kind
// dotnet run --project src/Capstone.App -- --recent          → the last few entries looked at, newest first
// dotnet run --project src/Capstone.App -- --online         → also asks PokeAPI for one species
// dotnet run --project src/Capstone.App -- --demo where     → Session 6's live demos (where · closure · crash · unsub): see Demos.cs

// Session 6's live demos run before anything subscribes: the crash demo needs a store nobody listens to.
var demoAt = Array.IndexOf(args, "--demo");
if (demoAt >= 0)
{
    Demos.Run(demoAt + 1 < args.Length ? args[demoAt + 1] : "", Path.Combine(AppContext.BaseDirectory, "data", "pokedex.json"));
    return;
}

var pokedex = new Pokedex();

// Lab 06: two subscribers, neither of which the Pokédex knows anything about.
var loaded = 0;
var lastFew = new RecentlyUsed<string>(3);
pokedex.ItemAdded += _ => loaded++;
pokedex.ItemAdded += item => lastFew.Push(item.DisplayName);
var path = Path.Combine(AppContext.BaseDirectory, "data", "pokedex.json");

using (var doc = JsonDocument.Parse(File.ReadAllText(path)))
{
    var mapper = new PokemonMapper();
    foreach (var element in doc.RootElement.EnumerateArray())
    {
        if (mapper.FromJson(element.GetRawText()) is { } pokemon)
            pokedex.Add(pokemon);
        else
            Console.WriteLine($"skipped invalid entry: {element.GetRawText()}");
    }
}

var ash = new Trainer { Name = "Ash" };
(pokedex.FindById("pkmn-025") as Pokemon)?.CaughtBy(ash);

if (args.Contains("--recent"))
{
    // The generic type doing its job: a "recently viewed" list that knows nothing about Pokémon.
    var recent = new RecentlyUsed<string>(3);
    foreach (var item in pokedex)          // foreach over the store itself — Lab 05
        recent.Push(item.DisplayName);

    Console.WriteLine($"Last {recent.Count} entries looked at, newest first:");
    foreach (var name in recent.Items())
        Console.WriteLine($"  {name}");
    return;
}

if (args.Contains("--kinds"))
{
    // Lab 04: one loop, one line per Pokémon, and the override decides what that line says.
    var wiring = new Lab04Wiring();
    foreach (var (line, item) in wiring.DescribeAll(pokedex).Zip(pokedex.All()))
        Console.WriteLine($"  [{wiring.Classify(item)}] {line}");
    return;
}

var searchAt = Array.IndexOf(args, "--search");
var query = searchAt >= 0 && searchAt + 1 < args.Length ? args[searchAt + 1] : "";
var shown = ItemSearch.Find(pokedex, query);

Console.WriteLine($"{loaded} entries announced while loading; last few: {string.Join(", ", lastFew.Items())}");
Console.WriteLine(query.Length == 0 ? $"{pokedex.Count} Pokémon loaded:" : $"{shown.Count} of {pokedex.Count} match \"{query}\":");
foreach (var item in shown)
    Console.WriteLine($"  {(item as IDescribable)?.Describe() ?? item.DisplayName}");

// A trainer isn't a Pokémon and isn't in the Pokédex, but it is IDescribable too: same call, different type.
IDescribable someone = ash;
Console.WriteLine(someone.Describe());

if (args.Contains("--online"))
{
    // One HttpClient for the program's lifetime. BaseAddress ends with '/', relative paths don't start with one.
    using var http = new HttpClient { BaseAddress = new Uri("https://pokeapi.co/api/v2/"), Timeout = TimeSpan.FromSeconds(10) };
    var api = new PokeApiClient(http);
    try
    {
        var eevee = await api.GetAsync("eevee");
        Console.WriteLine(eevee is null ? "PokeAPI doesn't know that one." : $"From PokeAPI: {eevee.Describe()}");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"PokeAPI unreachable: {ex.Message}");
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("PokeAPI took too long.");
    }
}
