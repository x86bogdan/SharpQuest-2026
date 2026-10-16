using System.Text.Json;
using Capstone.Core.Json;
using Capstone.Core.Model;
using Capstone.Core.Search;
using Capstone.Core.Storage;
using Capstone.Core.Web;

// Lab 02–03 reference: load the Pokédex from JSON, skip what's invalid, show what's left, search it.
// dotnet run --project src/Capstone.App                     → local file only
// dotnet run --project src/Capstone.App -- --search "fire"  → only the matches
// dotnet run --project src/Capstone.App -- --online         → also asks PokeAPI for one species

var pokedex = new Pokedex();
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

var searchAt = Array.IndexOf(args, "--search");
var query = searchAt >= 0 && searchAt + 1 < args.Length ? args[searchAt + 1] : "";
var shown = ItemSearch.Find(pokedex, query);

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
