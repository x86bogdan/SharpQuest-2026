# Lab 04 — reference solution

Lay this folder's `src/` (and `tests/`) over your own only if it's your theme. Otherwise read it, don't copy it.

| File | Shows |
|---|---|
| `src/Capstone.Core/Model/Pokemon.cs` | the same class as Lab 02, now `abstract`, with `abstract string Describe()` and a `protected Card()` for the shared part |
| `src/Capstone.Core/Model/WildPokemon.cs` | a kind with its own property (`Habitat`, blank-safe) and its own description |
| `src/Capstone.Core/Model/CaughtPokemon.cs` | a kind with a required `Keeper`; its `init` also calls `CaughtBy`, so the trainer's `LastCaught` stays in step (a private backing field, as in Lab 02) |
| `src/Capstone.Core/Json/PokemonMapper.cs` | **the whole migration**: one extra branch — JSON with a trainer becomes `CaughtPokemon`, without becomes `WildPokemon` |
| `src/Capstone.Core/Json/PokemonDto.cs` | one new optional property (`Trainer`), so old JSON still loads |
| `src/Capstone.Core/Wiring/Lab04Wiring.cs` | `Describe` through the base · a `foreach` that never tests a type · `Classify` as a switch expression with `null` and `_` arms · `TryFindByName` with `out` · `AddAll` with `params` |
| `src/Capstone.App/Program.cs` | `--kinds` prints `[label] description` for the whole Pokédex |
| `tests/Capstone.Tests/PokemonTests.cs` | six new own-code tests over the kinds and the mapper's choice |
