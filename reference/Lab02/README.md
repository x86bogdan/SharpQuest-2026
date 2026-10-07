# Lab 02 — reference solution

Lay this folder's `src/` (and `tests/`) over your own only if it's your theme. Otherwise read it, don't copy it.

| File | Shows |
|---|---|
| `Model/Pokemon.cs` | `required`/`init`, validation in the setter over a private backing field, a nullable that means something (`Nickname`) |
| `Model/PokemonType.cs`, `IDescribable.cs`, `Trainer.cs` | enum, own interface, a second class. `Pokemon` and `Trainer` both implement `IDescribable`: two unrelated types, one shared capability |
| `Storage/Pokedex.cs` | list + ordinal dictionary kept in step; snapshot via `ToArray()` |
| `Json/PokemonDto.cs`, `PokemonMapper.cs` | DTO record with all-nullable members; validate, then construct; `JsonException` → null |
| `Web/PokeApiClient.cs` | the stretch task: injected `HttpClient`, 404 → null, other failures throw, cancellation |
| `Wiring/Lab02Wiring.cs` | the contract wiring |
| `Capstone.App/Program.cs` | loads `data/pokedex.json`, skips the invalid entry; `--online` calls PokeAPI |
