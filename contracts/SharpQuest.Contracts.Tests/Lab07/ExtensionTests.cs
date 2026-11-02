using System.Reflection;
using System.Runtime.CompilerServices;
using SharpQuest.Contracts.Tests.Support;

namespace SharpQuest.Contracts.Tests.Lab07;

/// <summary>
/// Lab 07: the mechanism behind LINQ. Every verb you used today is a static method
/// whose first parameter is the sequence — and so is the one you wrote.
/// </summary>
public class ExtensionTests : Lab07TestBase
{
    private Type Host => Wiring.ExtensionHost
        ?? Fail<Type>("ExtensionHost is null. Return the static class that declares your Longest extension.");

    [Fact]
    public void Longest_puts_the_longest_name_first()
    {
        var items = Items(("a", "Ivysaur"), ("b", "Bulbasaur"), ("c", "Mew"));

        Assert.Equal("b,a,c", Ids(Wiring.Longest(items, 3)));
    }

    [Fact]
    public void Longest_breaks_a_tie_with_the_id()
    {
        var items = Items(("z", "Eevee"), ("a", "Ditto"), ("m", "Gengar"));

        var actual = Ids(Wiring.Longest(items, 3));

        Assert.True(actual == "m,a,z",
            $"Three names, two of them the same length, came back as {actual}, expected m,a,z. " +
            "Longest first, and equal lengths are broken by Id.");
    }

    [Fact]
    public void Asking_for_one_gives_the_longest()
    {
        var items = Mixed();

        Assert.Equal(Ids(InLengthOrder(items).Take(1)), Ids(Wiring.Longest(items, 1)));
    }

    [Fact]
    public void Asking_for_more_than_there_are_gives_all_of_them()
    {
        var items = Mixed();

        Assert.Equal(Ids(InLengthOrder(items)), Ids(Wiring.Longest(items, 99)));
    }

    [Fact]
    public void Asking_for_none_gives_none()
    {
        Assert.Empty(Wiring.Longest(Mixed(), 0));
    }

    [Fact]
    public void A_negative_count_is_a_programming_error()
    {
        var ex = Record.Exception(() => Wiring.Longest(Mixed(), -1));

        Assert.True(ex is not null, "Longest(items, -1) returned instead of throwing.");
        Assert.IsAssignableFrom<ArgumentOutOfRangeException>(ex);
    }

    [Fact]
    public void Longest_of_nothing_is_nothing()
    {
        Assert.Empty(Wiring.Longest([], 3));
    }

    [Fact]
    public void Longest_of_a_null_sequence_is_a_programming_error()
    {
        var ex = Record.Exception(() => Wiring.Longest(null!, 1));

        Assert.True(ex is not null, "Longest(null, 1) returned instead of throwing.");
        Assert.IsAssignableFrom<ArgumentNullException>(ex);
    }

    [Fact]
    public void The_extension_host_is_a_static_class_of_your_own()
    {
        var host = Host;

        Assert.True(host.Assembly == StudentCode.Assembly,
            $"{host.Name} lives in {host.Assembly.GetName().Name}, not {StudentCode.AssemblyName}. " +
            "Your extensions belong in your own domain library.");
        Assert.True(host is { IsAbstract: true, IsSealed: true, IsClass: true },
            $"{host.Name} is not a static class. Extension methods can only live in one.");
        Assert.False(StudentCode.IsWiring(host),
            $"{host.Name} is your wiring class. Put the extension in a class of its own — the wiring only points at your code.");
    }

    [Fact]
    public void It_declares_an_extension_method_on_a_sequence_of_items()
    {
        var found = Extensions(Host);

        Assert.True(found.Count > 0,
            $"{Host.Name} declares no extension method whose first parameter is a sequence of items. " +
            "The shape is: public static … Longest(this IEnumerable<IItem> items, int n) — `this` is the whole trick. " +
            "A method inside a C# 14 extension(IEnumerable<IItem> items) block counts too.");
    }

    [Fact]
    public void Calling_your_extension_directly_gives_what_Longest_gives()
    {
        var items = Mixed();
        var expected = Ids(Wiring.Longest(items, 2));
        var tried = new List<string>();

        foreach (var method in Extensions(Host).Where(m => m.GetParameters().Length == 2
                                                        && m.GetParameters()[1].ParameterType == typeof(int)))
        {
            try
            {
                if (method.Invoke(null, [items, 2]) is IEnumerable<IItem> result && Ids(result) == expected)
                    return;
                tried.Add($"{method.Name} gave something else");
            }
            catch (TargetInvocationException ex)
            {
                tried.Add($"{method.Name} threw {ex.InnerException?.GetType().Name}");
            }
        }

        Assert.Fail(tried.Count == 0
            ? $"{Host.Name} has no extension method taking (sequence, int), so Longest cannot be going through one. " +
              "Write Longest as the extension and have the wiring call it."
            : $"No extension in {Host.Name} answers Longest(items, 2) the way the wiring does ({string.Join("; ", tried)}). " +
              "The wiring method should be one line: return items.Longest(n).ToList();");
    }

    /// <summary>Static methods marked with `this`, whose first parameter accepts a sequence of items.</summary>
    private static List<MethodInfo> Extensions(Type host)
    {
        var sequence = typeof(IEnumerable<IItem>);
        var found = new List<MethodInfo>();

        foreach (var method in host.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (!method.IsDefined(typeof(ExtensionAttribute), inherit: false)) continue;

            var candidate = method;
            if (method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1)
            {
                try { candidate = method.MakeGenericMethod(typeof(IItem)); }
                catch (ArgumentException) { continue; }
            }

            var first = candidate.GetParameters().FirstOrDefault();
            if (first is not null && first.ParameterType.IsAssignableFrom(sequence))
                found.Add(candidate);
        }

        return found;
    }

    private static T Fail<T>(string message)
    {
        Assert.Fail(message);
        return default!;   // never reached
    }
}
