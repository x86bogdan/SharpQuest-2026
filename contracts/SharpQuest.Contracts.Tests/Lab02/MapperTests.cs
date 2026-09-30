using System.Text.Json;
using SharpQuest.Contracts.Tests.Support;

namespace SharpQuest.Contracts.Tests.Lab02;

/// <summary>Lab 02: JSON mapping through <see cref="IItemMapper"/>.</summary>
public class MapperTests
{
    // Resolved inside each test (not in the constructor) so a missing wiring class skips the test.
    private ILab02Wiring? _wiringCache;
    private ILab02Wiring Wiring => _wiringCache ??= StudentCode.Wiring<ILab02Wiring>();

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(42)]
    public void Round_trip_keeps_id_and_display_name(int seed)
    {
        var mapper = Wiring.CreateMapper();
        var original = Wiring.CreateSample(seed);

        var copy = mapper.FromJson(mapper.ToJson(original));

        Assert.NotNull(copy);
        Assert.Equal(original.Id, copy.Id);
        Assert.Equal(original.DisplayName, copy.DisplayName);
    }

    [Fact]
    public void ToJson_produces_a_json_object()
    {
        var json = Wiring.CreateMapper().ToJson(Wiring.CreateSample(1));

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{")]
    [InlineData("not json at all")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("42")]
    public void Invalid_json_gives_null_and_does_not_throw(string json)
    {
        var mapper = Wiring.CreateMapper();

        var ex = Record.Exception(() => mapper.FromJson(json));
        if (ex is not null)
            Assert.Fail($"FromJson threw {ex.GetType().Name} for input {Show(json)}. Bad input should give null, not an exception.");

        var result = mapper.FromJson(json);
        Assert.True(result is null, $"FromJson accepted {Show(json)} and returned {result}. It should return null.");
    }

    [Fact]
    public void An_empty_object_is_missing_required_data()
    {
        var result = Wiring.CreateMapper().FromJson("{}");
        Assert.True(result is null, $"FromJson accepted {{}} and returned {result}. An item with no Id should be rejected.");
    }

    [Fact]
    public void Json_with_a_blank_id_is_rejected()
    {
        var mapper = Wiring.CreateMapper();
        var json = mapper.ToJson(Wiring.CreateSample(1));
        var id = Wiring.CreateSample(1).Id;

        // Replace the id value wherever it appears as a JSON string.
        var blanked = json.Replace(JsonSerializer.Serialize(id), "\"\"");
        Assert.True(json != blanked, $"Could not find the Id {JsonSerializer.Serialize(id)} as a JSON string in: {json}");

        IItem? result = null;
        var ex = Record.Exception(() => result = mapper.FromJson(blanked));
        if (ex is not null)
            Assert.Fail($"FromJson threw {ex.GetType().Name} for a blank Id. Check the DTO first and return null.");
        Assert.True(result is null, $"FromJson accepted an item with a blank Id: {blanked}");
    }

    private static string Show(string json) => json.Length == 0 ? "(empty string)" : $"'{json}'";
}
