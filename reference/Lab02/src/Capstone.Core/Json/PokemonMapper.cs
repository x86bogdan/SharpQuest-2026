using System.Text.Json;
using System.Text.Json.Serialization;
using Capstone.Core.Model;
using SharpQuest.Contracts;

namespace Capstone.Core.Json;

public sealed class PokemonMapper : IItemMapper
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string ToJson(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is not Pokemon p)
            throw new ArgumentException($"This mapper only knows Pokémon, not {item.GetType().Name}.", nameof(item));

        return JsonSerializer.Serialize(ToDto(p), Options);
    }

    public IItem? FromJson(string json)
    {
        PokemonDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<PokemonDto>(json, Options);
        }
        catch (JsonException)
        {
            return null; // not JSON, or not an object, or a value of the wrong type
        }

        return dto is null ? null : FromDto(dto);
    }

    public static PokemonDto ToDto(Pokemon p) =>
        new(p.Id, p.Species, p.PrimaryType, p.SecondaryType, p.Level, p.Nickname);

    /// <summary>Null when the DTO is missing something a Pokémon cannot exist without.</summary>
    public static Pokemon? FromDto(PokemonDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id)) return null;
        if (string.IsNullOrWhiteSpace(dto.Species)) return null;
        if (dto.PrimaryType is not { } primary) return null;
        if (dto.Level is < 1 or > 100) return null;

        return new Pokemon
        {
            Id = dto.Id,
            Species = dto.Species,
            PrimaryType = primary,
            SecondaryType = dto.SecondaryType,
            Level = dto.Level ?? 1,
            Nickname = dto.Nickname,
        };
    }
}
