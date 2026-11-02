using SharpQuest.Contracts.Tests.Support;

namespace SharpQuest.Contracts.Tests.Lab08;

/// <summary>
/// Lab 08: `async` that means something. A method can return a `Task`, satisfy the interface,
/// and still hold a thread hostage the whole time it runs — so these read the compiled code
/// rather than the signature.
/// </summary>
public class AsyncTests : Lab08TestBase
{
    [Theory]
    [InlineData(nameof(ILab08Wiring.SaveAsync))]
    [InlineData(nameof(ILab08Wiring.LoadAsync))]
    public void It_waits_for_real_io(string method)
    {
        _ = Wiring;   // "not started" rather than a wall of red
        var reach = IoUsage.Student.From(Wiring.GetType(), method);

        Assert.True(reach.AsyncIo.Count > 0,
            $"{method} never reaches an asynchronous file call. Returning a Task isn't the point — " +
            "the point is that the thread is free while the disk works. Use File.ReadAllTextAsync / " +
            "WriteAllTextAsync, a StreamReader's ReadToEndAsync, or JsonSerializer.SerializeAsync, " +
            "and await it.");
    }

    [Theory]
    [InlineData(nameof(ILab08Wiring.SaveAsync))]
    [InlineData(nameof(ILab08Wiring.LoadAsync))]
    public void It_does_not_block_on_the_task(string method)
    {
        _ = Wiring;
        var reach = IoUsage.Student.From(Wiring.GetType(), method);

        Assert.True(reach.Blocking.Count == 0,
            $"{method} blocks on a task ({string.Join(", ", reach.Blocking)}). That gives up everything async was " +
            "for — the thread waits anyway, and in a UI (next week) or a web server (two weeks) it's the classic " +
            "way to deadlock. Await it instead, all the way up.");
    }

    [Theory]
    [InlineData(nameof(ILab08Wiring.SaveAsync))]
    [InlineData(nameof(ILab08Wiring.LoadAsync))]
    public void It_is_your_own_work(string method)
    {
        _ = Wiring;
        var reach = IoUsage.Student.From(Wiring.GetType(), method);

        Assert.False(reach.CatchUp,
            $"{method} routes through the catch-up kit ({StudentCode.CatchUpNamespace}). The kit exists so a missed " +
            "lab doesn't stop the next one — it isn't a way to do this lab. Write your own and call that.");
    }
}
