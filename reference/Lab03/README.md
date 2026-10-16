# Lab 03 — reference solution

Lay this folder's `src/` (and `tests/`) over your own only if it's your theme. Otherwise read it, don't copy it.

| File | Shows |
|---|---|
| `tests/Capstone.Tests/SearchMatcherTests.cs` | one test per rule R1–R8; theories where one rule has several examples; catches all 8 broken matchers |
| `tests/Capstone.Tests/PokemonTests.cs` | tests of rules only this domain has: level boundaries (theory + `Assert.Throws`), blank species, nickname normalisation, `DisplayName`, `CaughtBy` with and without a trainer, DTO validation, exact Id lookup |
| `tests/Capstone.Tests/ItemSearchTests.cs` | search that also matches a Pokémon's types; null store rejected |
| `src/Capstone.Core/Search/ItemSearch.cs` | search over the store with `SearchMatcher`, in store order, matching the display name plus types |
| `src/Capstone.Core/Wiring/Lab03Wiring.cs` | the contract wiring, reusing the Lab 02 wiring for store and samples |
| `src/Capstone.App/Program.cs` | `--search "fire"` shows only the matches |
