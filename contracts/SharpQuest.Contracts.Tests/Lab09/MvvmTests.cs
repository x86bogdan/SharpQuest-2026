using SharpQuest.Contracts.Tests.Support;

namespace SharpQuest.Contracts.Tests.Lab09;

/// <summary>
/// Lab 09: the reason any of the above can be tested at all. The view model is ordinary C# that has
/// never heard of a Button — so a test runner on a Linux build server can drive your whole window.
/// </summary>
public class MvvmTests : Lab09TestBase
{
    private static readonly string[] UiFrameworks =
        ["Avalonia", "PresentationFramework", "PresentationCore", "WindowsBase", "System.Windows.Forms", "Microsoft.Maui"];

    [Fact]
    public void The_view_model_lives_in_your_domain_library()
    {
        var type = Wiring.CreateViewModel(Wiring.CreateStore()).GetType();

        Assert.True(type.Assembly == StudentCode.Assembly,
            $"{type.Name} lives in {type.Assembly.GetName().Name}, not {StudentCode.AssemblyName}. " +
            "Put it with the rest of your model: the UI project should hold a window and almost nothing else.");
    }

    [Fact]
    public void Your_domain_library_has_never_heard_of_a_button()
    {
        _ = Wiring;   // "not started" rather than a lone green check on an empty template

        var referenced = StudentCode.Assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? "")
            .Where(name => UiFrameworks.Any(ui => name.StartsWith(ui, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.True(referenced.Count == 0,
            $"{StudentCode.AssemblyName} references {string.Join(", ", referenced)}. The whole point of a view model " +
            "is that it doesn't know what it is being shown on — which is also why these tests can run it at all, " +
            "on a build server with no screen.");
    }

    [Fact]
    public void The_view_model_never_blocks()
    {
        var type = Wiring.CreateViewModel(Wiring.CreateStore()).GetType();

        var blocking = BlockingCalls.Student.In(type);

        Assert.True(blocking.Count == 0,
            $"{type.Name} blocks on a task ({string.Join(", ", blocking)}). Last week that was wasteful; here it is " +
            "the freeze the user sees, because the thread you are holding is the one that draws the window. " +
            "If loading has to happen, await it — and give the window something to show in the meantime.");
    }
}
