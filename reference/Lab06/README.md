# Lab 06 — reference solution

Lay this folder's `src/` (and `tests/`) over your own only if it's your theme. Otherwise read it, don't copy it.

| File | Shows |
|---|---|
| `src/Capstone.Core/Storage/Pokedex.cs` | `public event Action<IItem>? ItemAdded;` raised with `?.Invoke` **after** the entry is really in — three added lines, nothing else touched |
| `src/Capstone.Core/Wiring/Lab06Wiring.cs` | `Subscribe` returning the unsubscribe as a closure; `Where`/`Map`/`ForEach` calling the caller's function once per item, in store order, with null guards |
| `src/Capstone.App/Program.cs` | two subscribers attached before loading — a counter and a `RecentlyUsed<string>` from Lab 05 — neither of which the Pokédex knows about |
| `src/Capstone.App/Demos.cs` | **Session 6's live demos** (added 2026-11-03), run with `dotnet run --project src/Capstone.App -- --demo where` · `closure` · `crash` · `unsub`. Each loads its own Pokédex with **nobody subscribed** (the normal run subscribes two handlers first, so deleting the `?` there never crashes). The code is the slides' code. `crash`: runs fine; delete the `?` in `Pokedex.Add` → build error CS8602 (nullable warnings are errors); write `!` → `NullReferenceException`. Checked 2026-11-03 against the published Lab 06 contract (SDK 10, built for net10.0 here: NuGet is blocked) |
| `tests/Capstone.Tests/PokedexEventTests.cs` | six own-code tests: the announcement carries the item, a rejected duplicate announces nothing, `-=` detaches only one handler, no subscribers is not a crash, `Count` is already right inside the handler, and a predicate passed in decides what comes back |
