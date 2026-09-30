using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpQuest.Contracts.Tests.Support;

/// <summary>
/// Finds things in the student's Capstone.Core assembly without knowing their names.
/// Shared by every lab's contract tests.
/// </summary>
public static class StudentCode
{
    public const string AssemblyName = "Capstone.Core";

    /// <summary>sq looks for this prefix to show a lab as "not started".</summary>
    public const string NotStartedPrefix = "Not started:";

    /// <summary>
    /// Where catch-up kits live. A kit is borrowed plumbing that unblocks the *next* lab;
    /// it never counts as having done the lab it came from.
    /// </summary>
    public const string CatchUpNamespace = "Capstone.Core.CatchUp";

    public static Assembly Assembly { get; } = Assembly.Load(new AssemblyName(AssemblyName));

    /// <summary>Is this type borrowed from a catch-up kit rather than written by the student?</summary>
    public static bool IsCatchUp(Type? type) =>
        type?.Namespace is { } ns && (ns == CatchUpNamespace || ns.StartsWith(CatchUpNamespace + ".", StringComparison.Ordinal));

    /// <summary>
    /// Creates the student's wiring object for a lab: the one class implementing <typeparamref name="TWiring"/>.
    /// No such class means the student hasn't started this lab: every test that asks is SKIPPED, not failed,
    /// so published-but-future labs show as "not started" instead of a wall of red.
    /// Call it inside a test method, never in a constructor.
    /// <para>
    /// Wiring classes inside a catch-up kit don't count — a kit is there to unblock later labs,
    /// not to hand back the lab it belongs to.
    /// </para>
    /// </summary>
    public static TWiring Wiring<TWiring>() where TWiring : class =>
        WiringIn<TWiring>(Assembly, $"src/{AssemblyName}/Wiring/");

    /// <summary>
    /// The student's front-end project for the web API (Lab 10 on). Null when there isn't one yet:
    /// the contract tests reference <c>src/Capstone.Api</c> only if it exists, so a repo without it
    /// still builds and every lab before Lab 10 reads exactly as it did.
    /// </summary>
    public const string ApiAssemblyName = "Capstone.Api";

    private static readonly Lazy<Assembly?> ApiAssemblyLazy = new(() =>
    {
        try { return Assembly.Load(new AssemblyName(ApiAssemblyName)); }
        catch (FileNotFoundException) { return null; }
    });

    public static Assembly? ApiAssembly => ApiAssemblyLazy.Value;

    /// <summary>
    /// Like <see cref="Wiring{TWiring}"/>, but for a lab whose wiring lives in <c>src/Capstone.Api</c>.
    /// No Api project yet means the lab hasn't started: skipped, not failed. A wiring written in
    /// <c>Capstone.Core</c> instead is a failure with directions, not a silent "not started".
    /// </summary>
    public static TWiring ApiWiring<TWiring>() where TWiring : class
    {
        var inCore = Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(TWiring).IsAssignableFrom(t) && !IsCatchUp(t))
            .ToList();
        if (inCore.Count > 0)
            Assert.Fail($"{inCore[0].Name} implements {typeof(TWiring).Name}, but it is in {AssemblyName}. " +
                        $"This lab's wiring belongs in src/{ApiAssemblyName}/ — an HTTP endpoint is a front end, like " +
                        $"the console app and the window, and your domain library shouldn't know HTTP exists.");

        if (ApiAssembly is null)
            Assert.Skip($"{NotStartedPrefix} there is no src/{ApiAssemblyName} project yet. Create it with " +
                        $"`dotnet new web -o src/{ApiAssemblyName}` to start this lab.");

        return WiringIn<TWiring>(ApiAssembly!, $"src/{ApiAssemblyName}/");   // Assert.Skip above never returns
    }

    private static TWiring WiringIn<TWiring>(Assembly assembly, string where) where TWiring : class
    {
        var all = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(TWiring).IsAssignableFrom(t))
            .ToList();

        var candidates = all.Where(t => !IsCatchUp(t)).ToList();

        if (candidates.Count == 0)
            Assert.Skip(all.Count > 0
                ? $"{NotStartedPrefix} the only class implementing {typeof(TWiring).Name} is the catch-up kit's " +
                  $"({all[0].FullName}). The kit unblocks later labs; it doesn't do this one. " +
                  $"Write your own in {where}."
                : $"{NotStartedPrefix} no class in {assembly.GetName().Name} implements {typeof(TWiring).Name}. " +
                  $"Add one to {where} to start this lab.");
        if (candidates.Count > 1)
            Assert.Fail($"{candidates.Count} classes implement {typeof(TWiring).Name} " +
                        $"({string.Join(", ", candidates.Select(c => c.Name))}). Keep exactly one.");

        var type = candidates[0];
        if (type.GetConstructor(Type.EmptyTypes) is null)
            Assert.Fail($"{type.Name} needs a public constructor with no parameters.");

        var wiring = (TWiring)Activator.CreateInstance(type)!;
        RejectKitObjects(wiring);
        return wiring;
    }

    /// <summary>
    /// A wiring class of your own that hands back the kit's objects is still the kit doing the work.
    /// Calls every simple factory the wiring exposes (<c>CreateStore()</c>, <c>CreateSample(seed)</c>, …)
    /// and fails if what comes back was borrowed.
    /// </summary>
    private static void RejectKitObjects(object wiring)
    {
        foreach (var method in wiring.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!method.Name.StartsWith("Create", StringComparison.Ordinal)) continue;
            if (method.ReturnType == typeof(void) || method.ReturnType.IsValueType) continue;

            var parameters = method.GetParameters();
            object?[] arguments;
            if (parameters.Length == 0) arguments = [];
            else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int)) arguments = [1];
            else continue;

            object? made;
            try { made = method.Invoke(wiring, arguments); }
            catch { continue; }   // a half-written factory is the lab's own tests' business, not this check's

            if (made is not null && IsCatchUp(made.GetType()))
                Assert.Fail($"{wiring.GetType().Name}.{method.Name}() returns {made.GetType().FullName}, " +
                            "which comes from the catch-up kit. The kit is there to unblock later labs — " +
                            "pointing your wiring at it doesn't earn this one. Write your own and return that.");
        }
    }

    /// <summary>
    /// The student's own model: every type they wrote, minus wiring classes, catch-up kit types,
    /// compiler-generated types and the contracts they were given.
    /// </summary>
    public static IReadOnlyList<Type> ModelTypes()
    {
        return Assembly.GetTypes()
            .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .Where(t => !t.Name.StartsWith('<'))                 // anonymous/closure types
            .Where(t => t.Name != "Program")
            .Where(t => !IsWiring(t))
            .Where(t => !IsCatchUp(t))
            .ToList();
    }

    public static bool IsWiring(Type t) =>
        t.GetInterfaces().Any(i => i.Namespace == "SharpQuest.Contracts" && i.Name.EndsWith("Wiring"));

    /// <summary>Records are classes or structs the compiler gave a synthesized EqualityContract / &lt;Clone&gt;$.</summary>
    public static bool IsRecord(Type t) =>
        t.GetMethod("<Clone>$") is not null                                    // record class
        || (t.IsValueType && t.GetMethod("PrintMembers",
               BindingFlags.Instance | BindingFlags.NonPublic) is not null     // record struct
            && t.GetMethods().Any(m => m.Name == "op_Equality"));
}
