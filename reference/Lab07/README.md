# Lab 07 — reference solution

Lay this folder's `src/` (and `tests/`) over your own only if it's your theme. Otherwise read it, don't copy it.

| File | Shows |
|---|---|
| `src/Capstone.Core/Queries/ItemQueries.cs` | the `Longest` **extension method** on `IEnumerable<IItem>`, plus two more, `OneLine()` and `IsEmpty()` (C# 14 could make those two *properties* with an `extension` block; the course builds with C# 13, so they're methods) |
| `src/Capstone.Core/Wiring/Lab07Wiring.cs` | the five queries. `Names` is a `Select` over the Pokédex **itself** (so it stays a query); `Sorted` ends in `ToList()` (so it doesn't); `Page` composes on `Sorted` and guards its arguments; `Longest` is one line through the extension; `Summary` answers for an empty sequence rather than letting `Average()` throw |
| `src/Capstone.App/Program.cs` | a query written *before the file is read*, answered after loading — and a `--queries` mode showing a page, the three longest names, the summary and the extension methods |
| `tests/Capstone.Tests/ItemQueriesTests.cs` | seven own-code tests over the new code, including a nickname changing which entry is longest |
