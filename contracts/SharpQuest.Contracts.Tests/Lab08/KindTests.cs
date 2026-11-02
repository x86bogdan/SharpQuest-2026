namespace SharpQuest.Contracts.Tests.Lab08;

/// <summary>
/// Lab 08: the part that's easy to get wrong. Your items aren't all the same kind, and a file
/// that doesn't say which kind each one is can only give you back whichever kind your loader builds.
/// </summary>
public class KindTests : Lab08TestBase
{
    [Fact]
    public async Task Each_kind_comes_back_as_its_own_kind()
    {
        var store = Wiring.CreateStore();
        foreach (var item in OneOfEachKind()) store.Add(item);

        var loaded = await SaveAndLoad(store);

        foreach (var saved in store.All())
        {
            var back = loaded.FindById(saved.Id);
            Assert.True(back is not null, $"'{saved.DisplayName}' never came back.");
            Assert.True(back!.GetType() == saved.GetType(),
                $"A {saved.GetType().Name} came back as a {back.GetType().Name}. The file has to record which kind " +
                "each item is — a type discriminator, a flag, a separate list, your choice — or every item comes back " +
                "as whichever kind the loader happens to construct.");
        }
    }

    [Fact]
    public async Task Each_kind_keeps_its_own_values()
    {
        var store = Wiring.CreateStore();
        foreach (var item in OneOfEachKind()) store.Add(item);

        var loaded = await SaveAndLoad(store);

        foreach (var saved in store.All())
            AssertSameItem(saved, loaded.FindById(saved.Id)!);
    }

    [Fact]
    public async Task A_mix_of_kinds_keeps_its_order()
    {
        var store = Wiring.CreateStore();
        var kinds = OneOfEachKind();
        foreach (var item in kinds) store.Add(item);
        store.Add(Wiring.CreateSample(77));

        var loaded = await SaveAndLoad(store);

        Assert.Equal(Ids(store.All()), Ids(loaded.All()));
    }

    [Fact]
    public async Task The_mix_is_still_the_same_mix()
    {
        var store = Wiring.CreateStore();
        foreach (var item in OneOfEachKind()) store.Add(item);

        var loaded = await SaveAndLoad(store);

        var before = string.Join(",", store.All().Select(i => i.GetType().Name).Order());
        var after = string.Join(",", loaded.All().Select(i => i.GetType().Name).Order());

        Assert.True(before == after,
            $"Saved [{before}] and loaded [{after}]. This is the classic failure of this lab: " +
            "System.Text.Json refuses to build an abstract type, so either it throws, or a loader that builds one " +
            "concrete kind for everything silently flattens the lot.");
    }
}
