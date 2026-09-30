# Wiring

Each lab's contract tests need to create *your* objects without knowing your class names.
You tell them how, with one small class per lab in this folder.

Session 2, for example, asks for a class implementing `ILab02Wiring`:

```csharp
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab02Wiring : ILab02Wiring
{
    public IItemStore CreateStore() => new MyStore();
    public IItemMapper CreateMapper() => new MyMapper();
    public IItem CreateSample(int seed) => new MyThing($"thing-{seed}", $"Thing number {seed}");
}
```

The name of the class does not matter. There must be exactly one per lab, and it needs a constructor with no parameters.
Wiring classes are not counted as part of your model.
