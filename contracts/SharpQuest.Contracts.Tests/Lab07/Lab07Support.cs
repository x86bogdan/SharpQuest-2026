using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using SharpQuest.Contracts.Tests.Support;

namespace SharpQuest.Contracts.Tests.Lab07;

/// <summary>Shared by every Lab 07 test class.</summary>
public abstract class Lab07TestBase
{
    private ILab07Wiring? _wiringCache;

    protected ILab07Wiring Wiring => _wiringCache ??= StudentCode.Wiring<ILab07Wiring>();

    /// <summary>An item the tests own, so a test can choose the display names it needs.</summary>
    private sealed record TestItem(string Id, string DisplayName) : IItem;

    /// <summary>Items with exactly these ids and display names. Never added to a student's store.</summary>
    protected static List<IItem> Items(params (string Id, string Name)[] pairs) =>
        pairs.Select(p => (IItem)new TestItem(p.Id, p.Name)).ToList();

    /// <summary>Four items whose order differs under every plausible rule.</summary>
    protected static List<IItem> Mixed() => Items(
        ("b", "banana"),
        ("z", "Apple"),
        ("a", "apple"),
        ("c", "cherry pie"));

    protected IItemStore StoreWith(params int[] seeds)
    {
        var store = Wiring.CreateStore();
        Assert.True(store is not null, "CreateStore() returned null.");
        foreach (var seed in seeds) store!.Add(Wiring.CreateSample(seed));
        return store!;
    }

    /// <summary>Name order: DisplayName ignoring case, ties broken by Id.</summary>
    protected static IReadOnlyList<IItem> InNameOrder(IEnumerable<IItem> items) =>
        items.OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
             .ThenBy(i => i.Id, StringComparer.Ordinal)
             .ToList();

    /// <summary>Length order: longest display name first, ties broken by Id.</summary>
    protected static IReadOnlyList<IItem> InLengthOrder(IEnumerable<IItem> items) =>
        items.OrderByDescending(i => i.DisplayName.Length)
             .ThenBy(i => i.Id, StringComparer.Ordinal)
             .ToList();

    protected static string Ids(IEnumerable<IItem> items) => string.Join(",", items.Select(i => i.Id));

    protected static string Names(IEnumerable<IItem> items) => string.Join(",", items.Select(i => i.DisplayName));
}

/// <summary>
/// Which <c>System.Linq</c> methods a student's method actually calls, read from the compiled
/// <c>Capstone.Core</c> assembly. Nothing is executed: this walks the IL, follows lambdas,
/// iterator state machines and helper methods in the same assembly, and collects the names.
/// </summary>
public sealed class LinqUsage
{
    /// <summary>The query methods that count as "a verb". Conversions like ToList don't.</summary>
    public static readonly HashSet<string> Verbs =
    [
        "Select", "SelectMany", "Where", "OfType",
        "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "Order", "OrderDescending", "Reverse",
        "Skip", "SkipWhile", "SkipLast", "Take", "TakeWhile", "TakeLast", "Chunk",
        "Distinct", "DistinctBy", "Union", "Intersect", "Except", "Concat", "Zip",
        "GroupBy", "GroupJoin", "Join", "ToLookup",
        "Count", "LongCount", "Any", "All", "Contains", "SequenceEqual",
        "First", "FirstOrDefault", "Last", "LastOrDefault", "Single", "SingleOrDefault", "ElementAt", "ElementAtOrDefault",
        "Min", "MinBy", "Max", "MaxBy", "Sum", "Average", "Aggregate",
    ];

    private static readonly Dictionary<ushort, OpCode> OpCodesByValue =
        typeof(OpCodes).GetFields().Select(f => (OpCode)f.GetValue(null)!).ToDictionary(o => (ushort)o.Value);

    private readonly MetadataReader _md;
    private readonly PEReader _pe;
    private readonly Dictionary<MethodDefinitionHandle, HashSet<string>> _cache = [];
    private readonly HashSet<MethodDefinitionHandle> _inProgress = [];

    private LinqUsage(MetadataReader md, PEReader pe)
    {
        _md = md;
        _pe = pe;
    }

    private static LinqUsage? _instance;

    /// <summary>The student's Capstone.Core, read once.</summary>
    public static LinqUsage Student => _instance ??= Load(StudentCode.Assembly.Location);

    public static LinqUsage Load(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var pe = new PEReader(stream);
        return new LinqUsage(pe.GetMetadataReader(), pe);
    }

    /// <summary>
    /// Every System.Linq method reachable from <paramref name="methodName"/> on <paramref name="declaringType"/>,
    /// including from its lambdas, its iterator body and helpers it calls in the same assembly.
    /// </summary>
    public IReadOnlyCollection<string> CallsFrom(Type declaringType, string methodName)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        var type = FindType(declaringType);
        if (type is null) return found;

        foreach (var mh in BodiesOf(type.Value, methodName))
            found.UnionWith(Scan(mh));
        return found;
    }

    /// <summary>The same, for every method the type declares (used for "anywhere in your extension class").</summary>
    public IReadOnlyCollection<string> CallsFromAnyMethodOf(Type declaringType)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        var type = FindType(declaringType);
        if (type is null) return found;

        foreach (var mh in _md.GetTypeDefinition(type.Value).GetMethods())
            found.UnionWith(Scan(mh));
        return found;
    }

    private TypeDefinitionHandle? FindType(Type type)
    {
        foreach (var th in _md.TypeDefinitions)
        {
            var td = _md.GetTypeDefinition(th);
            if (_md.GetString(td.Name) == type.Name && _md.GetString(td.Namespace) == (type.Namespace ?? ""))
                return th;
        }
        return null;
    }

    /// <summary>
    /// The method itself, plus the compiler-generated bodies that belong to it: lambdas
    /// (<c>&lt;Name&gt;b__0</c>) and iterator state machines (<c>&lt;Name&gt;d__0.MoveNext</c>),
    /// where the real work of a <c>yield</c> method lives.
    /// </summary>
    private IEnumerable<MethodDefinitionHandle> BodiesOf(TypeDefinitionHandle th, string methodName)
    {
        var marker = $"<{methodName}>";
        var stack = new Stack<TypeDefinitionHandle>([th]);
        while (stack.Count > 0)
        {
            var t = _md.GetTypeDefinition(stack.Pop());
            var typeName = _md.GetString(t.Name);
            foreach (var mh in t.GetMethods())
            {
                var name = _md.GetString(_md.GetMethodDefinition(mh).Name);
                var isTheMethod = name == methodName || name.EndsWith("." + methodName, StringComparison.Ordinal);
                if (isTheMethod || name.Contains(marker, StringComparison.Ordinal) || typeName.Contains(marker, StringComparison.Ordinal))
                    yield return mh;
            }
            foreach (var nested in t.GetNestedTypes()) stack.Push(nested);
        }
    }

    private HashSet<string> Scan(MethodDefinitionHandle mh)
    {
        if (_cache.TryGetValue(mh, out var cached)) return cached;
        var found = new HashSet<string>(StringComparer.Ordinal);
        if (!_inProgress.Add(mh)) return found;

        var method = _md.GetMethodDefinition(mh);
        if (method.RelativeVirtualAddress != 0)
        {
            var il = _pe.GetMethodBody(method.RelativeVirtualAddress).GetILReader();
            while (il.RemainingBytes > 0)
            {
                int code = il.ReadByte();
                if (code == 0xFE) code = 0xFE00 | il.ReadByte();
                if (!OpCodesByValue.TryGetValue((ushort)code, out var op)) break;
                switch (op.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar: il.ReadByte(); break;
                    case OperandType.InlineVar: il.ReadUInt16(); break;
                    case OperandType.InlineI or OperandType.InlineBrTarget or OperandType.InlineSig or OperandType.InlineString: il.ReadInt32(); break;
                    case OperandType.ShortInlineR: il.ReadSingle(); break;
                    case OperandType.InlineR: il.ReadDouble(); break;
                    case OperandType.InlineI8: il.ReadInt64(); break;
                    case OperandType.InlineSwitch: var n = il.ReadInt32(); for (var i = 0; i < n; i++) il.ReadInt32(); break;
                    default:
                        found.UnionWith(Target(MetadataTokens.EntityHandle(il.ReadInt32())));
                        break;
                }
            }
        }

        _inProgress.Remove(mh);
        return _cache[mh] = found;
    }

    private HashSet<string> Target(EntityHandle h)
    {
        switch (h.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                // A helper of their own: follow it, and follow its lambdas and iterator body too.
                var def = _md.GetMethodDefinition((MethodDefinitionHandle)h);
                var found = new HashSet<string>(StringComparer.Ordinal);
                foreach (var body in BodiesOf(def.GetDeclaringType(), _md.GetString(def.Name)))
                    found.UnionWith(Scan(body));
                return found;
            }
            case HandleKind.MethodSpecification:
                return Target(_md.GetMethodSpecification((MethodSpecificationHandle)h).Method);
            case HandleKind.MemberReference:
            {
                var mr = _md.GetMemberReference((MemberReferenceHandle)h);
                var owner = TypeNameOf(mr.Parent);
                return owner is "System.Linq.Enumerable" or "System.Linq.Queryable"
                    ? [_md.GetString(mr.Name)]
                    : [];
            }
            default:
                return [];
        }
    }

    private string TypeNameOf(EntityHandle h)
    {
        switch (h.Kind)
        {
            case HandleKind.TypeReference:
            {
                var tr = _md.GetTypeReference((TypeReferenceHandle)h);
                var ns = _md.GetString(tr.Namespace);
                var name = _md.GetString(tr.Name);
                return ns.Length == 0 ? name : $"{ns}.{name}";
            }
            case HandleKind.TypeDefinition:
            {
                var td = _md.GetTypeDefinition((TypeDefinitionHandle)h);
                var ns = _md.GetString(td.Namespace);
                var name = _md.GetString(td.Name);
                return ns.Length == 0 ? name : $"{ns}.{name}";
            }
            default:
                return "";
        }
    }
}
