using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using SharpQuest.Contracts.Tests.Support;

namespace SharpQuest.Contracts.Tests.Lab09;

/// <summary>Shared by every Lab 09 test class.</summary>
public abstract class Lab09TestBase
{
    private ILab09Wiring? _wiringCache;

    protected ILab09Wiring Wiring => _wiringCache ??= StudentCode.Wiring<ILab09Wiring>();

    protected IItemStore StoreWith(params int[] seeds)
    {
        var store = Wiring.CreateStore();
        Assert.True(store is not null, "CreateStore() returned null.");
        foreach (var seed in seeds) store!.Add(Wiring.CreateSample(seed));
        return store!;
    }

    /// <summary>A store and a view model over it.</summary>
    protected (IItemStore Store, ICatalogueViewModel Vm) Open(params int[] seeds)
    {
        var store = StoreWith(seeds);
        var vm = Wiring.CreateViewModel(store);
        Assert.True(vm is not null, "CreateViewModel(store) returned null.");
        return (store, vm!);
    }

    protected static string Ids(IEnumerable<IItem> items) => string.Join(",", items.Select(i => i.Id));

    protected static IReadOnlyList<IItem> Shown(ICatalogueViewModel vm) => vm.Items.ToList();

    /// <summary>Records the property names a view model announces.</summary>
    protected sealed class Announced : IDisposable
    {
        private readonly ICatalogueViewModel _vm;
        public List<string> Names { get; } = [];

        public Announced(ICatalogueViewModel vm)
        {
            _vm = vm;
            _vm.PropertyChanged += Handle;
        }

        private void Handle(object? sender, PropertyChangedEventArgs e) => Names.Add(e.PropertyName ?? "(null)");

        public bool Saw(string property) => Names.Contains(property);

        public string What => Names.Count == 0 ? "nothing at all" : string.Join(", ", Names);

        public void Dispose() => _vm.PropertyChanged -= Handle;
    }

    /// <summary>Records what an observable collection announced.</summary>
    protected sealed class Watched : IDisposable
    {
        private readonly INotifyCollectionChanged? _source;
        public List<NotifyCollectionChangedAction> Actions { get; } = [];

        public Watched(IEnumerable<IItem> items)
        {
            _source = items as INotifyCollectionChanged;
            if (_source is not null) _source.CollectionChanged += Handle;
        }

        public bool IsObservable => _source is not null;

        private void Handle(object? sender, NotifyCollectionChangedEventArgs e) => Actions.Add(e.Action);

        public void Dispose()
        {
            if (_source is not null) _source.CollectionChanged -= Handle;
        }
    }

    protected static T Fail<T>(string message)
    {
        Assert.Fail(message);
        return default!;   // never reached
    }
}

/// <summary>
/// Does the view model block a thread, read from the compiled <c>Capstone.Core</c>?
/// In a console app that was merely wasteful; in a window it is the freeze the user sees.
/// Nothing is executed — this walks the IL of every method the type declares, including
/// lambdas and async state machines.
/// </summary>
public sealed class BlockingCalls
{
    private static readonly HashSet<string> Blocking = ["Wait", "WaitAll", "WaitAny", "get_Result"];

    private static readonly Dictionary<ushort, OpCode> OpCodesByValue =
        typeof(OpCodes).GetFields().Select(f => (OpCode)f.GetValue(null)!).ToDictionary(o => (ushort)o.Value);

    private readonly MetadataReader _md;
    private readonly PEReader _pe;

    private BlockingCalls(MetadataReader md, PEReader pe) { _md = md; _pe = pe; }

    private static BlockingCalls? _instance;

    public static BlockingCalls Student => _instance ??= Load(StudentCode.Assembly.Location);

    public static BlockingCalls Load(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var pe = new PEReader(stream);
        return new BlockingCalls(pe.GetMetadataReader(), pe);
    }

    /// <summary>Every blocking call this type makes, anywhere in it.</summary>
    public IReadOnlyCollection<string> In(Type type)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var th in _md.TypeDefinitions)
        {
            var td = _md.GetTypeDefinition(th);
            var name = _md.GetString(td.Name);
            var ns = _md.GetString(td.Namespace);

            // The type itself, and the nested types the compiler made for its lambdas and awaits.
            var isTheType = name == type.Name && ns == (type.Namespace ?? "");
            var isNestedInIt = IsNestedIn(th, type);
            if (!isTheType && !isNestedInIt) continue;

            foreach (var mh in td.GetMethods()) found.UnionWith(Scan(mh));
        }

        return found;
    }

    private bool IsNestedIn(TypeDefinitionHandle th, Type type)
    {
        var td = _md.GetTypeDefinition(th);
        var declaring = td.GetDeclaringType();
        if (declaring.IsNil) return false;
        var outer = _md.GetTypeDefinition(declaring);
        return _md.GetString(outer.Name) == type.Name && _md.GetString(outer.Namespace) == (type.Namespace ?? "");
    }

    private HashSet<string> Scan(MethodDefinitionHandle mh)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        var method = _md.GetMethodDefinition(mh);
        if (method.RelativeVirtualAddress == 0) return found;

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
                    var member = Member(MetadataTokens.EntityHandle(il.ReadInt32()));
                    if (member is not null) found.Add(member);
                    break;
            }
        }
        return found;
    }

    private string? Member(EntityHandle h)
    {
        if (h.Kind == HandleKind.MethodSpecification)
            return Member(_md.GetMethodSpecification((MethodSpecificationHandle)h).Method);
        if (h.Kind != HandleKind.MemberReference) return null;

        var mr = _md.GetMemberReference((MemberReferenceHandle)h);
        var member = _md.GetString(mr.Name);
        if (!Blocking.Contains(member)) return null;

        var owner = TypeNameOf(mr.Parent);
        return owner is "System.Threading.Tasks.Task" or "System.Threading.Tasks.Task`1"
                     or "System.Threading.Tasks.ValueTask" or "System.Threading.Tasks.ValueTask`1"
            ? (member == "get_Result" ? ".Result" : $".{member}()")
            : null;
    }

    private string TypeNameOf(EntityHandle h) => h.Kind switch
    {
        HandleKind.TypeReference => Full(_md.GetTypeReference((TypeReferenceHandle)h)),
        HandleKind.TypeSpecification =>
            _md.GetTypeSpecification((TypeSpecificationHandle)h).DecodeSignature(new BareNames(), null),
        _ => "",
    };

    private string Full(TypeReference tr)
    {
        var ns = _md.GetString(tr.Namespace);
        var name = _md.GetString(tr.Name);
        return ns.Length == 0 ? name : $"{ns}.{name}";
    }

    /// <summary>Signature types as "namespace.Name", generic arguments discarded.</summary>
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
