namespace SharpQuest.Contracts.Tests.Lab07;

/// <summary>
/// Lab 07: these are already written. The point of the lab isn't the answers — you could get
/// those with a foreach, and did, last week. It's that the verbs exist, compose and read well.
/// So these checks read your compiled code and look for them.
/// </summary>
public class LinqTests : Lab07TestBase
{
    [Theory]
    [InlineData(nameof(ILab07Wiring.Names))]
    [InlineData(nameof(ILab07Wiring.Sorted))]
    [InlineData(nameof(ILab07Wiring.Page))]
    [InlineData(nameof(ILab07Wiring.Longest))]
    [InlineData(nameof(ILab07Wiring.Summary))]
    public void Every_query_goes_through_System_Linq(string method)
    {
        _ = Wiring;   // "not started" rather than a wall of red
        var used = LinqUsage.Student.CallsFrom(Wiring.GetType(), method);

        Assert.True(used.Count > 0,
            $"{method} doesn't call anything in System.Linq — it looks hand-written. " +
            "Today's point is that these already exist: Select, OrderBy, Skip, Take, Average and the rest.");
    }

    [Fact]
    public void The_five_queries_use_at_least_five_different_verbs()
    {
        var wiring = Wiring.GetType();
        var used = new[] { nameof(ILab07Wiring.Names), nameof(ILab07Wiring.Sorted), nameof(ILab07Wiring.Page),
                           nameof(ILab07Wiring.Longest), nameof(ILab07Wiring.Summary) }
            .SelectMany(m => LinqUsage.Student.CallsFrom(wiring, m))
            .Concat(LinqUsage.Student.CallsFromAnyMethodOf(Wiring.ExtensionHost ?? wiring))
            .Where(LinqUsage.Verbs.Contains)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.True(used.Count >= 5,
            $"Between them your five queries use {used.Count} different LINQ verb(s) ({string.Join(", ", used)}). " +
            "Five questions, five verbs at least — ToList and ToArray are conversions, not questions.");
    }
}
