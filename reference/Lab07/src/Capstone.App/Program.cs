using System.Text.Json;
using Capstone.Core.Collections;
using Capstone.Core.Json;
using Capstone.Core.Model;
using Capstone.Core.Queries;
using Capstone.Core.Search;
using Capstone.Core.Storage;
using Capstone.Core.Wiring;
using Capstone.Core.Web;

// Lab 02–03 reference: load the Pokédex from JSON, skip what's invalid, show what's left, search it.
// dotnet run --project src/Capstone.App                     → local file only
// dotnet run --project src/Capstone.App -- --search "fire"  → only the matches
// dotnet run --project src/Capstone.App -- --kinds          → what each one is, sorted by kind
// dotnet run --project src/Capstone.App -- --recent          → the last few entries looked at, newest first
// dotnet run --project src/Capstone.App -- --queries         → a page, the longest names, and the whole thing in one line
// dotnet run --project src/Capstone.App -- --online         → also asks PokeAPI for one species

var pokedex = new Pokedex();

// Lab 06: two subscribers, neither of which the Pokédex knows anything about.
var loaded = 0;
var lastFew = new RecentlyUsed<string>(3);
pokedex.ItemAdded += _ => loaded++;
pokedex.ItemAdded += item => lastFew.Push(item.DisplayName);

// Lab 07: a query written before a single entry exists. It isn't a result — it's a question,
// and it gets answered against whatever the Pokédex holds when somebody enumerates it, below.
var queries = new Lab07Wiring();
var everyName = queries.Names(pokedex);

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

if (args.Contains("--queries"))
{
    // Lab 07: five questions, none of which needed a loop.
    var all = pokedex.All();

    Console.WriteLine("The first page, three at a time, by name:");
    foreach (var item in queries.Page(all, 0, 3))
        Console.WriteLine($"  {item.DisplayName}");

    Console.WriteLine("The three longest names:");
    foreach (var item in all.Longest(3))                     // the extension method, reading like a member
        Console.WriteLine($"  {item.DisplayName}");

    var stats = queries.Summary(all);
    Console.WriteLine($"{stats.Count} entries · {stats.DistinctInitials} different initials · " +
                      $"longest \"{stats.LongestName}\" · {stats.AverageNameLength:0.0} characters on average");
    Console.WriteLine($"All of them: {all.OneLine()}");        // an extension method of our own
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
Console.WriteLine($"The query written before the file was read now answers: {string.Join(", ", everyName)}");
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
