using SharpQuest.Contracts.Tests.Support;

namespace SharpQuest.Contracts.Tests.Lab02;

/// <summary>
/// Lab 02, required feature: "3+ types, 1 enum, 1 interface, zero nullable warnings".
/// Zero nullable warnings is enforced by the build itself (Directory.Build.props), not here.
/// </summary>
public class ModelTests
{
    [Fact]
    public void Wiring_class_can_be_created()
    {
        var wiring = StudentCode.Wiring<ILab02Wiring>();
        Assert.NotNull(wiring);
    }

    [Fact]
    public void Model_has_at_least_three_types_of_your_own()
    {
        Started();
        var types = StudentCode.ModelTypes();
        Assert.True(types.Count >= 3,
            $"Found {types.Count} type(s) in your model ({Names(types)}). Session 2 asks for at least 3.");
    }

    [Fact]
    public void Model_has_an_enum()
    {
        Started();
        var enums = StudentCode.ModelTypes().Where(t => t.IsEnum).ToList();
        Assert.True(enums.Count >= 1, "Your model has no enum. Which property of your items has a fixed set of values?");
    }

    [Fact]
    public void Model_has_an_interface_of_your_own()
    {
        Started();
        var interfaces = StudentCode.ModelTypes().Where(t => t.IsInterface).ToList();
        Assert.True(interfaces.Count >= 1,
            "Your model declares no interface of its own. The ones in contracts/ don't count.");
    }

    [Fact]
    public void Model_has_a_record()
    {
        Started();
        var records = StudentCode.ModelTypes().Where(StudentCode.IsRecord).ToList();
        Assert.True(records.Count >= 1,
            "Your model has no record. Your DTOs are a natural place for one.");
    }

    [Fact]
    public void Items_are_your_own_type_not_a_contract_type()
    {
        var item = StudentCode.Wiring<ILab02Wiring>().CreateSample(1);
        Assert.Equal(StudentCode.AssemblyName, item.GetType().Assembly.GetName().Name);
    }

    // The shape checks don't need the wiring, but a lab without one counts as not started.
    private static void Started() => StudentCode.Wiring<ILab02Wiring>();

    private static string Names(IEnumerable<Type> types) =>
        string.Join(", ", types.Select(t => t.Name).DefaultIfEmpty("none"));
}
