using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using SharpQuest.Contracts.Tests.Support;

namespace SharpQuest.Contracts.Tests.Lab05;

/// <summary>Shared by every Lab 05 test class: the wiring, the collection type, and small helpers.</summary>
public abstract class Lab05TestBase
{
    private ILab05Wiring? _wiringCache;

    protected ILab05Wiring Wiring => _wiringCache ??= StudentCode.Wiring<ILab05Wiring>();

    protected Type CollectionType
    {
        get
        {
            var type = Wiring.CollectionType ?? Fail<Type>("CollectionType is null. Return the class CreateStore() gives back.");
            for (var t = type; t is not null; t = t.BaseType)
                if (StudentCode.IsCatchUp(t))
                    Assert.Fail($"CollectionType is {type.FullName}, which {(t == type ? "is" : "derives from")} the catch-up kit's " +
                                $"{t.Name}. The kit unblocks later labs; it doesn't do this one. " +
                                "Make your own store the collection and return that.");
            return type;
        }
    }

    protected IItemStore StoreWith(params int[] seeds)
    {
        var store = Wiring.CreateStore();
        Assert.True(store is not null, "CreateStore() returned null.");
        foreach (var seed in seeds) store!.Add(Wiring.CreateSample(seed));
        return store!;
    }

    /// <summary>Indexers, whatever the student called them ("Item" by default, or renamed with IndexerName).</summary>
    protected static PropertyInfo? Indexer(Type type, Type parameterType) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.GetIndexParameters() is [{ } only] && only.ParameterType == parameterType);

    /// <summary>
    /// True when the type contains a method written with <c>yield return</c>: the compiler turns one into a
    /// nested state-machine class implementing IEnumerator. Handing back another collection's enumerator
    /// leaves no such type behind.
    /// </summary>
    protected static bool HasIteratorMember(Type type) =>
        type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Any(t => t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                      && typeof(IEnumerator).IsAssignableFrom(t));

    protected static string Name(Type t) => t.Name;

    protected static T Fail<T>(string message)
    {
        Assert.Fail(message);
        return default!;   // never reached
    }
}

/// <summary>
/// Does the student's code lean on a catch-up kit? Read from the compiled <c>Capstone.Core</c>, nothing executed:
/// walks the IL of a type's methods, follows lambdas, iterator bodies and helpers in the same assembly,
/// and reports the first kit member it reaches. The same idea as Lab 08's check, for Lab 05.
/// </summary>
public sealed class KitReach
{
    private static readonly Dictionary<ushort, OpCode> OpCodesByValue =
        typeof(OpCodes).GetFields().Select(f => (OpCode)f.GetValue(null)!).ToDictionary(o => (ushort)o.Value);

    private readonly MetadataReader _md;
    private readonly PEReader _pe;
    private readonly Dictionary<MethodDefinitionHandle, string?> _cache = [];
    private readonly HashSet<MethodDefinitionHandle> _inProgress = [];

    private KitReach(MetadataReader md, PEReader pe) { _md = md; _pe = pe; }

    private static KitReach? _instance;

    public static KitReach Student => _instance ??= Load(StudentCode.Assembly.Location);

    public static KitReach Load(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var pe = new PEReader(stream);
        return new KitReach(pe.GetMetadataReader(), pe);
    }

    /// <summary>"Type.Member" of the first kit member any method of <paramref name="type"/> reaches, or null.</summary>
    public string? FirstKitMemberReachedBy(Type type)
    {
        foreach (var th in _md.TypeDefinitions)
        {
            var td = _md.GetTypeDefinition(th);
            if (_md.GetString(td.Name) != type.Name || _md.GetString(td.Namespace) != (type.Namespace ?? "")) continue;

            foreach (var body in AllBodies(th))
                if (Scan(body) is { } found) return found;
        }
        return null;
    }

    /// <summary>Every method of the type, including the compiler-generated nested ones (lambdas, iterators).</summary>
    private IEnumerable<MethodDefinitionHandle> AllBodies(TypeDefinitionHandle th)
    {
        var stack = new Stack<TypeDefinitionHandle>([th]);
        while (stack.Count > 0)
        {
            var t = _md.GetTypeDefinition(stack.Pop());
            foreach (var mh in t.GetMethods()) yield return mh;
            foreach (var nested in t.GetNestedTypes()) stack.Push(nested);
        }
    }

    /// <summary>
    /// One method plus the compiler-generated bodies that belong to it: lambdas (<c>&lt;Name&gt;b__0</c>)
    /// and iterator state machines (<c>&lt;Name&gt;d__0.MoveNext</c>), where a <c>yield</c> method's work lives.
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

    private bool IsKit(TypeDefinitionHandle th)
    {
        // A nested type's namespace is empty in metadata: ask the outermost declaring type.
        var td = _md.GetTypeDefinition(th);
        while (td.GetDeclaringType() is { IsNil: false } outer) td = _md.GetTypeDefinition(outer);
        var ns = _md.GetString(td.Namespace);
        return ns == StudentCode.CatchUpNamespace || ns.StartsWith(StudentCode.CatchUpNamespace + ".", StringComparison.Ordinal);
    }

    private string? Scan(MethodDefinitionHandle mh)
    {
        if (_cache.TryGetValue(mh, out var cached)) return cached;
        if (!_inProgress.Add(mh)) return null;

        string? found = null;
        var method = _md.GetMethodDefinition(mh);
        if (method.RelativeVirtualAddress != 0)
        {
            var il = _pe.GetMethodBody(method.RelativeVirtualAddress).GetILReader();
            while (found is null && il.RemainingBytes > 0)
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
                        found = Target(MetadataTokens.EntityHandle(il.ReadInt32()));
                        break;
                }
            }
        }

        _inProgress.Remove(mh);
        return _cache[mh] = found;
    }

    private string? Target(EntityHandle h)
    {
        switch (h.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                var def = _md.GetMethodDefinition((MethodDefinitionHandle)h);
                var owner = def.GetDeclaringType();
                if (IsKit(owner))
                    return $"{_md.GetString(_md.GetTypeDefinition(owner).Name)}.{_md.GetString(def.Name)}";

                // A helper of their own (or their store): follow it, with its lambdas and iterator bodies.
                foreach (var body in BodiesOf(owner, _md.GetString(def.Name)))
                    if (Scan(body) is { } found) return found;
                return null;
            }
            case HandleKind.MethodSpecification:
                return Target(_md.GetMethodSpecification((MethodSpecificationHandle)h).Method);
            case HandleKind.TypeDefinition:
                // newarr, castclass, isinst, box … on a kit type
                return IsKit((TypeDefinitionHandle)h) ? _md.GetString(_md.GetTypeDefinition((TypeDefinitionHandle)h).Name) : null;
            default:
                return null;
        }
    }
}
