using System.Text.Json;
using Capstone.Core.Json;
using Capstone.Core.Model;
using Capstone.Core.Storage;
using Capstone.Core.Web;

// Lab 02 reference: load the Pokédex from JSON, skip what's invalid, show what's left.
// dotnet run --project src/Capstone.App              → local file only
// dotnet run --project src/Capstone.App -- --online  → also asks PokeAPI for one species

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

Console.WriteLine($"{pokedex.Count} Pokémon loaded:");
foreach (var item in pokedex.All())
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
