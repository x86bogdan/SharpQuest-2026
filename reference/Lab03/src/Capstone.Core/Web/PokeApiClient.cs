using System.Net;
using System.Net.Http.Json;
using Capstone.Core.Model;

namespace Capstone.Core.Web;

/// <summary>
/// Reads species data from https://pokeapi.co. The HttpClient is passed in, never created here:
/// one HttpClient for the whole program, not one per call.
/// </summary>
public sealed class PokeApiClient(HttpClient http)
{
    /// <summary>The species, or null if PokeAPI has never heard of it.</summary>
    public async Task<Pokemon?> GetAsync(string species, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"pokemon/{Uri.EscapeDataString(species.ToLowerInvariant())}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode(); // anything else unexpected is an exception, not a null

        var body = await response.Content.ReadFromJsonAsync<ApiPokemon>(cancellationToken);
        if (body is null || body.Types.Count == 0)
            return null;

        var types = body.Types.OrderBy(t => t.Slot).Select(t => ParseType(t.Type.Name)).ToList();

        return new Pokemon
        {
            Id = $"pkmn-{body.Id:D3}",
            Species = char.ToUpperInvariant(body.Name[0]) + body.Name[1..],
            PrimaryType = types[0] ?? PokemonType.Normal,
            SecondaryType = types.Count > 1 ? types[1] : null,
        };
    }

    private static PokemonType? ParseType(string name) =>
        Enum.TryParse<PokemonType>(name, ignoreCase: true, out var t) ? t : null;

    // Only the parts of PokeAPI's response we use. Everything else is ignored by the deserializer.
    private sealed record ApiPokemon(int Id, string Name, List<ApiTypeSlot> Types);
    private sealed record ApiTypeSlot(int Slot, ApiNamed Type);
    private sealed record ApiNamed(string Name);
}
