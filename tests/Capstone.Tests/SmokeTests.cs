namespace Capstone.Tests;

// Session 3 is about writing tests like this one, over your own logic.
// Until then, this one exists so the project has something to run.
public class SmokeTests
{
    [Fact]
    public void Arithmetic_still_works()
    {
        Assert.Equal(4, 2 + 2);
    }
}
