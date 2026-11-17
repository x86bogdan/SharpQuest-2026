using Capstone.Core.Search;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab03Wiring : ILab03Wiring
{
    private readonly Lab02Wiring _lab02 = new();

    public IItemStore CreateStore() => _lab02.CreateStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    public IReadOnlyList<IItem> Search(IItemStore store, string query) => ItemSearch.Find(store, query);
}
