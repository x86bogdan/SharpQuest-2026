// ─────────────────────────────────────────────────────────────────────────────
// Catch-up kit — Lab 08, persistence.
//
// Copy this file into src/Capstone.Core/CatchUp/ ONLY if you missed session 8 and need
// session 9 to have something to save and load. Then:
//
//     var file = new CatchUpFile(new YourMapper(), () => new YourStore());
//     await file.SaveAsync(store, "data/capstone.json");
//     var store = await file.LoadAsync("data/capstone.json");
//
// It is deliberately plain, it is not your theme's code, and it earns you nothing for Lab 08 —
// the contract tests know which namespace it lives in and say so by name. To get those points,
// write your own save and load for your own model, stop using this file, and ask to be verified
// at the start of a later lab: it counts as late work, worth up to 8.
// ─────────────────────────────────────────────────────────────────────────────

using System.Text.Json;
using SharpQuest.Contracts;

namespace Capstone.Core.CatchUp;

/// <summary>
/// Saves any store and loads it back, using the mapper you already wrote in Lab 02.
/// Each item is stored as the text your mapper produces, so whatever your model does with
/// kinds and fields keeps working — this file never needs to know about them.
/// </summary>
public sealed class CatchUpFile(IItemMapper mapper, Func<IItemStore> newStore)
{
    private readonly IItemMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly Func<IItemStore> _newStore = newStore ?? throw new ArgumentNullException(nameof(newStore));

    public async Task SaveAsync(IItemStore store, string path)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { Length: > 0 } folder)
            Directory.CreateDirectory(folder);

        var rows = store.All().Select(_mapper.ToJson).ToList();

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, rows);
    }

    public async Task<IItemStore> LoadAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var store = _newStore();
        if (!File.Exists(path)) return store;

        List<string>? rows;
        try
        {
            await using var stream = File.OpenRead(path);
            rows = await JsonSerializer.DeserializeAsync<List<string>>(stream);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{Path.GetFileName(path)} isn't a file this wrote.", ex);
        }

        foreach (var row in rows ?? [])
        {
            var item = _mapper.FromJson(row)
                ?? throw new InvalidDataException($"{Path.GetFileName(path)} has a line your mapper can't read.");
            store.Add(item);
        }

        return store;
    }
}
