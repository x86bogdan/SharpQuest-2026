using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using SharpQuest.Contracts.Tests.Support;

namespace SharpQuest.Contracts.Tests.Lab08;

/// <summary>Shared by every Lab 08 test class.</summary>
public abstract class Lab08TestBase : IDisposable
{
    private ILab08Wiring? _wiringCache;
    private readonly List<string> _scratch = [];

    protected ILab08Wiring Wiring => _wiringCache ??= StudentCode.Wiring<ILab08Wiring>();

    protected IItemStore StoreWith(params int[] seeds)
    {
        var store = Wiring.CreateStore();
        Assert.True(store is not null, "CreateStore() returned null.");
        foreach (var seed in seeds) store!.Add(Wiring.CreateSample(seed));
        return store!;
    }

    /// <summary>One of the student's items per kind they declared in Lab 04.</summary>
    protected IReadOnlyList<IItem> OneOfEachKind()
    {
        var kinds = Wiring.KindTypes;
        Assert.True(kinds is { Count: >= 2 },
            $"KindTypes lists {kinds?.Count ?? 0} kinds. Lab 04 asked for at least two — return the same list here.");

        var items = new List<IItem>();
        for (var i = 0; i < kinds!.Count; i++)
        {
            var item = Wiring.CreateOfKind(i, 40 + i);
            Assert.True(item is not null, $"CreateOfKind({i}, …) returned null.");
            items.Add(item!);
        }
        return items;
    }

    /// <summary>A path in a scratch folder of this test's own, cleaned up afterwards.</summary>
    protected string NewPath(string fileName = "capstone.json")
    {
        var dir = Path.Combine(Path.GetTempPath(), "sharpquest-lab08", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _scratch.Add(dir);
        return Path.Combine(dir, fileName);
    }

    /// <summary>A path whose folder does NOT exist yet — SaveAsync is expected to make it.</summary>
    protected string NewPathInMissingFolder(string fileName = "capstone.json")
    {
        var dir = Path.Combine(Path.GetTempPath(), "sharpquest-lab08", Guid.NewGuid().ToString("N"));
        _scratch.Add(dir);
        return Path.Combine(dir, "not-created-yet", fileName);
    }

    protected async Task<IItemStore> SaveAndLoad(IItemStore store, string? path = null)
    {
        path ??= NewPath();
        await Wiring.SaveAsync(store, path);
        Assert.True(File.Exists(path),
            $"SaveAsync returned but no file appeared at {Path.GetFileName(path)}. Did it write anywhere?");
        IItemStore? loaded;
        try
        {
            loaded = await Wiring.LoadAsync(path);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Saving worked, and then loading the file it had just written threw " +
                        $"{ex.GetType().Name}: {ex.Message}\n" +
                        "Your own loader should always be able to read your own file. The usual cause is that save " +
                        "writes one shape and load expects another — classically, handing the store (or a list of " +
                        "IItem) straight to JsonSerializer, which writes only what IItem declares: an Id and a " +
                        "display name, and none of what makes your items yours.");
            throw;
        }

        Assert.True(loaded is not null, "LoadAsync returned null. An empty store is still a store.");
        return loaded!;
    }

    protected static string Ids(IEnumerable<IItem> items) => string.Join(",", items.Select(i => i.Id));

    /// <summary>
    /// The exception an async call produced, however it produced it. A guard that runs before the
    /// first <c>await</c> throws straight away; one inside an <c>async</c> method comes back as a
    /// faulted task instead. Both are correct, so both count.
    /// <para>
    /// The wiring is resolved before the try, so an unstarted lab still skips rather than being
    /// caught here and reported as a failure.
    /// </para>
    /// </summary>
    protected async Task<Exception?> Caught(Func<ILab08Wiring, Task> call)
    {
        var wiring = Wiring;
        try { await call(wiring); return null; }
        catch (Exception ex) { return ex; }
    }

    /// <summary>
    /// The check this whole lab is built around: the item that came back is the same *kind* of thing,
    /// with the same values, as the one that went in.
    /// </summary>
    protected static void AssertSameItem(IItem saved, IItem loaded)
    {
        Assert.True(loaded.GetType() == saved.GetType(),
            $"'{saved.DisplayName}' was saved as a {saved.GetType().Name} and came back as a {loaded.GetType().Name}. " +
            "Each kind has to be written down in a way that says which kind it is, or they all come back as whichever " +
            "one your loader happens to build.");

        foreach (var property in saved.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || !property.CanRead) continue;

            object? before, after;
            try { before = property.GetValue(saved); after = property.GetValue(loaded); }
            catch { continue; }

            if (IsScalar(property.PropertyType))
            {
                Assert.True(Equals(before, after),
                    $"'{saved.DisplayName}' went in with {property.Name} = {Show(before)} and came back with " +
                    $"{Show(after)}. Everything your items carry has to survive the round trip — " +
                    "this is exactly what goes missing when the store object is handed straight to JsonSerializer, " +
                    "because it writes the declared element type (IItem: an Id and a display name) and nothing else.");
            }
            else
            {
                Assert.True((before is null) == (after is null),
                    $"'{saved.DisplayName}' went in with {property.Name} " +
                    $"{(before is null ? "not set" : "set")} and came back with it " +
                    $"{(after is null ? "not set" : "set")}.");
            }
        }
    }

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan) || type == typeof(Guid);
    }

    private static string Show(object? value) => value switch
    {
        null => "nothing",
        string s => $"\"{s}\"",
        _ => value.ToString() ?? "nothing",
    };

    public void Dispose()
    {
        foreach (var dir in _scratch)
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { /* scratch */ }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// What a student's save/load actually does, read from the compiled <c>Capstone.Core</c>:
/// does it await real I/O, does it block on a task, does it lean on a catch-up kit?
/// Nothing is executed — this walks the IL, following lambdas, async state machines
/// and helper methods in the same assembly.
/// </summary>
public sealed class IoUsage
{
    /// <summary>What one method reaches.</summary>
    public sealed record Reach(HashSet<string> AsyncIo, HashSet<string> Blocking, bool CatchUp)
    {
        public static Reach Empty() => new([], [], false);

        public Reach Merge(Reach other)
        {
            AsyncIo.UnionWith(other.AsyncIo);
            Blocking.UnionWith(other.Blocking);
            return this with { CatchUp = CatchUp || other.CatchUp };
        }
    }

    /// <summary>Blocking a thread on a task. `await` never compiles to any of these.</summary>
    private static readonly HashSet<string> BlockingMembers = ["Wait", "WaitAll", "WaitAny", "get_Result"];

    private static readonly Dictionary<ushort, OpCode> OpCodesByValue =
        typeof(OpCodes).GetFields().Select(f => (OpCode)f.GetValue(null)!).ToDictionary(o => (ushort)o.Value);

    private readonly MetadataReader _md;
    private readonly PEReader _pe;
    private readonly Dictionary<MethodDefinitionHandle, Reach> _cache = [];
    private readonly HashSet<MethodDefinitionHandle> _inProgress = [];

    private IoUsage(MetadataReader md, PEReader pe) { _md = md; _pe = pe; }

    private static IoUsage? _instance;

    public static IoUsage Student => _instance ??= Load(StudentCode.Assembly.Location);

    public static IoUsage Load(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var pe = new PEReader(stream);
        return new IoUsage(pe.GetMetadataReader(), pe);
    }

    public Reach From(Type declaringType, string methodName)
    {
        var reach = Reach.Empty();
        var type = FindType(declaringType);
        if (type is null) return reach;

        foreach (var body in BodiesOf(type.Value, methodName))
            reach = reach.Merge(Scan(body));
        return reach;
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
    /// The method itself, plus the compiler-generated bodies that belong to it. For an
    /// <c>async</c> method that is where all the real code lives: <c>&lt;SaveAsync&gt;d__3.MoveNext</c>.
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

    private Reach Scan(MethodDefinitionHandle mh)
    {
        if (_cache.TryGetValue(mh, out var cached)) return cached;
        var reach = Reach.Empty();
        if (!_inProgress.Add(mh)) return reach;

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
                        reach = reach.Merge(Target(MetadataTokens.EntityHandle(il.ReadInt32())));
                        break;
                }
            }
        }

        _inProgress.Remove(mh);
        return _cache[mh] = reach;
    }

    private Reach Target(EntityHandle h)
    {
        switch (h.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                // Their own helper: follow it, and follow its lambdas and async body too.
                var def = _md.GetMethodDefinition((MethodDefinitionHandle)h);
                var owner = _md.GetTypeDefinition(def.GetDeclaringType());
                var ns = _md.GetString(owner.Namespace);
                var borrowed = ns == StudentCode.CatchUpNamespace
                            || ns.StartsWith(StudentCode.CatchUpNamespace + ".", StringComparison.Ordinal);

                var reach = Reach.Empty() with { CatchUp = borrowed };
                foreach (var body in BodiesOf(def.GetDeclaringType(), _md.GetString(def.Name)))
                    reach = reach.Merge(Scan(body));
                return reach;
            }
            case HandleKind.MethodSpecification:
                return Target(_md.GetMethodSpecification((MethodSpecificationHandle)h).Method);
            case HandleKind.MemberReference:
            {
                var mr = _md.GetMemberReference((MemberReferenceHandle)h);
                var owner = TypeNameOf(mr.Parent);
                var member = _md.GetString(mr.Name);
                var reach = Reach.Empty();

                var isIo = owner.StartsWith("System.IO.", StringComparison.Ordinal)
                        || owner.StartsWith("System.Text.Json", StringComparison.Ordinal);
                if (isIo && member.EndsWith("Async", StringComparison.Ordinal))
                    reach.AsyncIo.Add($"{owner.Split('.')[^1]}.{member}");

                if (owner is "System.Threading.Tasks.Task" or "System.Threading.Tasks.Task`1"
                          or "System.Threading.Tasks.ValueTask" or "System.Threading.Tasks.ValueTask`1"
                    && BlockingMembers.Contains(member))
                    reach.Blocking.Add(member == "get_Result" ? ".Result" : $".{member}()");

                return reach;
            }
            default:
                return Reach.Empty();
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
            case HandleKind.TypeSpecification:
                // A generic instantiation: Task<List<Row>>.Result is the common way to block,
                // and it reaches here rather than as a plain type reference.
                return _md.GetTypeSpecification((TypeSpecificationHandle)h).DecodeSignature(new BareNames(), null);
            default:
                return "";
        }
    }

    /// <summary>Signature types as "namespace.Name", with the generic arguments thrown away.</summary>
    private sealed class BareNames : ISignatureTypeProvider<string, object?>
    {
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => "System." + typeCode;
        public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var t = r.GetTypeDefinition(handle);
            var ns = r.GetString(t.Namespace);
            return ns.Length == 0 ? r.GetString(t.Name) : $"{ns}.{r.GetString(t.Name)}";
        }
        public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var t = r.GetTypeReference(handle);
            var ns = r.GetString(t.Namespace);
            return ns.Length == 0 ? r.GetString(t.Name) : $"{ns}.{r.GetString(t.Name)}";
        }
        public string GetTypeFromSpecification(MetadataReader r, object? ctx, TypeSpecificationHandle handle, byte rawTypeKind) =>
            r.GetTypeSpecification(handle).DecodeSignature(this, ctx);
        public string GetGenericInstantiation(string genericType, System.Collections.Immutable.ImmutableArray<string> typeArguments) => genericType;
        public string GetSZArrayType(string elementType) => elementType;
        public string GetArrayType(string elementType, ArrayShape shape) => elementType;
        public string GetByReferenceType(string elementType) => elementType;
        public string GetPointerType(string elementType) => elementType;
        public string GetPinnedType(string elementType) => elementType;
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
        public string GetGenericTypeParameter(object? ctx, int index) => "!T" + index;
        public string GetGenericMethodParameter(object? ctx, int index) => "!M" + index;
        public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";
    }
}
