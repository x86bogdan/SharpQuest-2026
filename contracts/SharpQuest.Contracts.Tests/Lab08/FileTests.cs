namespace SharpQuest.Contracts.Tests.Lab08;

/// <summary>
/// Lab 08: files are the outside world. Nothing saved yet is normal; a file full of nonsense is not.
/// Telling those two apart is most of what `try`/`catch` around I/O is for.
/// </summary>
public class FileTests : Lab08TestBase
{
    [Fact]
    public async Task A_file_that_is_not_there_is_an_empty_store()
    {
        var path = NewPath("never-written.json");

        IItemStore? loaded = null;
        var ex = await Caught(async w => loaded = await w.LoadAsync(path));

        Assert.True(ex is null,
            $"Loading a file that doesn't exist threw {ex?.GetType().Name}. It should give an empty store: " +
            "it's the first run of every program ever written — not an error, just nothing saved yet.");
        Assert.True(loaded is not null && loaded.Count == 0,
            $"Loading a file that doesn't exist gave {loaded?.Count.ToString() ?? "null"} instead of an empty store.");
    }

    [Fact]
    public async Task Loading_a_missing_file_does_not_create_one()
    {
        var path = NewPath("never-written.json");

        await Caught(w => w.LoadAsync(path));

        Assert.False(File.Exists(path), "Loading created the file. Reading shouldn't write.");
    }

    [Fact]
    public async Task A_folder_that_is_not_there_is_an_empty_store_too()
    {
        IItemStore? loaded = null;
        var path = NewPathInMissingFolder();
        var ex = await Caught(async w => loaded = await w.LoadAsync(path));

        Assert.True(ex is null,
            $"A path whose folder doesn't exist threw {ex?.GetType().Name}. It is still just 'nothing saved yet' — " +
            "File.Exists says false for a file in a folder that isn't there, so one check covers both.");
        Assert.True(loaded is not null && loaded.Count == 0, "…and it should be an empty store.");
    }

    [Fact]
    public async Task Saving_makes_the_folder_it_needs()
    {
        var path = NewPathInMissingFolder();

        var store = StoreWith(1, 2);
        var ex = await Caught(w => w.SaveAsync(store, path));

        Assert.True(ex is null,
            $"Saving into a folder that doesn't exist yet threw {ex?.GetType().Name}. " +
            "Directory.CreateDirectory on the folder part of the path first — telling a user to go and make a " +
            "folder before your program will save is rude.");
        Assert.True(File.Exists(path),
            "Saving into a folder that doesn't exist yet threw it away. Directory.CreateDirectory on the folder " +
            "part of the path first — telling a user to go and make a folder before your program will save is rude.");
    }

    [Fact]
    public async Task Nonsense_in_the_file_is_an_error()
    {
        var path = NewPath();
        await File.WriteAllTextAsync(path, "{ this is not json at all ");

        var ex = await Caught(w => w.LoadAsync(path));

        Assert.True(ex is not null,
            "A file full of nonsense loaded without complaint. Missing data and corrupt data are different problems: " +
            "returning an empty store here is how someone loses a semester's work and doesn't find out for a week.");
        Assert.IsAssignableFrom<InvalidDataException>(ex);
    }

    [Fact]
    public async Task Json_of_the_wrong_shape_is_an_error()
    {
        var path = NewPath();
        await File.WriteAllTextAsync(path, """{ "hello": "world" }""");

        var ex = await Caught(w => w.LoadAsync(path));

        Assert.True(ex is not null, "Valid JSON that isn't your data loaded without complaint.");
        Assert.IsAssignableFrom<InvalidDataException>(ex);
    }

    [Fact]
    public async Task Half_a_file_is_an_error()
    {
        // What a crash mid-save leaves behind.
        var path = NewPath();
        await Wiring.SaveAsync(StoreWith(1, 2, 3), path);
        var whole = await File.ReadAllTextAsync(path);
        await File.WriteAllTextAsync(path, whole[..(whole.Length / 2)]);

        var ex = await Caught(w => w.LoadAsync(path));

        Assert.True(ex is not null, "A file that stops halfway through loaded without complaint.");
        Assert.IsAssignableFrom<InvalidDataException>(ex);
    }

    [Fact]
    public async Task Save_rejects_a_missing_store()
    {
        var path = NewPath();
        var ex = await Caught(w => w.SaveAsync(null!, path));

        Assert.True(ex is not null, "SaveAsync(null, path) finished without complaining.");
        Assert.IsAssignableFrom<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task Save_rejects_a_missing_path()
    {
        var store = StoreWith(1);
        var ex = await Caught(w => w.SaveAsync(store, null!));

        Assert.True(ex is not null, "SaveAsync(store, null) finished without complaining.");
        Assert.IsAssignableFrom<ArgumentNullException>(ex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Save_rejects_a_blank_path(string path)
    {
        var store = StoreWith(1);
        var ex = await Caught(w => w.SaveAsync(store, path));

        Assert.True(ex is not null, $"SaveAsync(store, \"{path}\") finished without complaining.");
        Assert.IsAssignableFrom<ArgumentException>(ex);
    }

    [Fact]
    public async Task Load_rejects_a_missing_path()
    {
        _ = Wiring;
        var ex = await Caught(w => w.LoadAsync(null!));

        Assert.True(ex is not null, "LoadAsync(null) finished without complaining.");
        Assert.IsAssignableFrom<ArgumentNullException>(ex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Load_rejects_a_blank_path(string path)
    {
        var ex = await Caught(w => w.LoadAsync(path));

        Assert.True(ex is not null, $"LoadAsync(\"{path}\") finished without complaining.");
        Assert.IsAssignableFrom<ArgumentException>(ex);
    }
}
