using System;
using System.Linq;
using NFun.Types;

namespace NFun;

/// <summary>
/// NFun type description
/// </summary>
public readonly struct FunnyType {

    internal static readonly FunnyType Empty = new();
    public static readonly FunnyType Any    = new(BaseFunnyType.Any);
    public static readonly FunnyType Ip     = new(BaseFunnyType.Ip);
    public static readonly FunnyType Bool   = new(BaseFunnyType.Bool);
    public static readonly FunnyType Char   = new(BaseFunnyType.Char);
    public static readonly FunnyType UInt8  = new(BaseFunnyType.UInt8);
    public static readonly FunnyType UInt16 = new(BaseFunnyType.UInt16);
    public static readonly FunnyType UInt32 = new(BaseFunnyType.UInt32);
    public static readonly FunnyType UInt64 = new(BaseFunnyType.UInt64);
    public static readonly FunnyType Int8   = new(BaseFunnyType.Int8);
    public static readonly FunnyType Int16  = new(BaseFunnyType.Int16);
    public static readonly FunnyType Int32  = new(BaseFunnyType.Int32);
    public static readonly FunnyType Int64  = new(BaseFunnyType.Int64);
    public static readonly FunnyType Float32 = new(BaseFunnyType.Float32);
    public static readonly FunnyType Real   = new(BaseFunnyType.Real);
    public static readonly FunnyType Text   = ArrayOf(Char);
    public static readonly FunnyType None   = new(BaseFunnyType.None);

    public static FunnyType PrimitiveOf(BaseFunnyType baseType) => new(baseType);

    public static FunnyType ArrayOf(FunnyType type) => new(type);

    /// <summary>
    /// Lang-mode growable list <c>list&lt;T&gt;</c>. Distinct from
    /// <see cref="ArrayOf"/> — the legacy ee-mode <c>T[]</c> stays unchanged.
    /// </summary>
    public static FunnyType ListOf(FunnyType type) => new(new ListTypeSpecification(type));

    /// <summary>
    /// Lang-mode mutable array <c>array&lt;T&gt;</c>. Fixed length, mutable
    /// element. Distinct from both <see cref="ArrayOf"/> (ee-mode immutable)
    /// and <see cref="ListOf"/> (growable).
    /// </summary>
    public static FunnyType MutableArrayOf(FunnyType type) => new(new MutableArrayTypeSpecification(type));

    /// <summary>
    /// Lang-mode immutable fixed-length array <c>fixedArray&lt;T&gt;</c>.
    /// Read-only after construction. Sits at the top of the Array-branch
    /// lattice: list and mutable array values flow into it, but it doesn't
    /// flow back down.
    /// </summary>
    public static FunnyType FixedArrayOf(FunnyType type) => new(new FixedArrayTypeSpecification(type));

    /// <summary>
    /// <para>Stage C — constraint-only generic container shape <c>Enumerable&lt;V&gt;</c>
    /// for function signatures (e.g. <c>count&lt;V&gt;(xs: Enumerable&lt;V&gt;)</c>).
    /// Accepts any concrete collection kind whose <see cref="Tic.SolvingStates.ConstructorKind"/>
    /// caps at <see cref="Tic.SolvingStates.ConstructorKind.Enumerable"/> — list, mutable
    /// array, fixed array, set, queue, map, or the legacy ee-mode <c>T[]</c>.</para>
    /// <para>Never instantiated as a value type. At graph-build time, occurrences in
    /// argument positions are converted to a <see cref="Tic.SolvingStates.StateCompositeConstraints"/>
    /// node with <c>Ancestor=Enumerable</c> and <c>ElementNode</c> bound to the
    /// resolved element generic per spec §6.2 dispatch flow.</para>
    /// </summary>
    public static FunnyType EnumerableOf(FunnyType type) => new(new EnumerableTypeSpecification(type));

    /// <summary>
    /// Lang-mode unordered hash <c>set&lt;T&gt;</c>. Invariant in element.
    /// Backed by <c>MutableFunnySet</c> (<c>HashSet&lt;object&gt;</c>). Sits on
    /// a separate branch from the List/Array/FixedArray chain — its only
    /// supertype is <see cref="EnumerableOf"/>.
    /// </summary>
    public static FunnyType SetOf(FunnyType type) => new(new SetTypeSpecification(type));

    /// <summary>
    /// Stage C — constraint-only typeclass <c>Mutable&lt;V&gt;</c>. Satisfied
    /// by <c>list&lt;V&gt;</c>, <c>array&lt;V&gt;</c>, <c>set&lt;V&gt;</c> (and
    /// future queue/stack). Rejected for <c>fixedArray&lt;V&gt;</c> /
    /// <c>T[]</c> ee-mode / <c>enumerable&lt;V&gt;</c>. Used for mutators like
    /// <c>clear</c>, future <c>addAll</c>, etc.
    /// </summary>
    public static FunnyType ClearableOf(FunnyType type) => new(new ClearableTypeSpecification(type));

    /// <summary>
    /// Lang-mode hash <c>map&lt;K, V&gt;</c>. Both arguments invariant. Backed
    /// by <see cref="NFun.Runtime.Lists.MutableFunnyMap"/>. Iteration over a
    /// map yields <c>{key, value}</c> structs (Stage C — runtime-level pair
    /// iteration; explicit destructure syntax is deferred).
    /// </summary>
    public static FunnyType MapOf(FunnyType keyType, FunnyType valueType)
        => new(new MapTypeSpecification(keyType, valueType));

    public static FunnyType OptionalOf(FunnyType type) =>
        // Idempotent for already-optional and for None (which renders as `any?` —
        // already an optional shape). Bug hunt round 5 #29.
        type.BaseType is BaseFunnyType.Optional or BaseFunnyType.None ? type : new(type, isOptional: true);

    /// <summary>
    /// Element type yielded by iterating this type as an Enumerable. For
    /// collection-shaped types returns their element; for Map returns the
    /// synthesised <c>{key, value}</c> pair-struct. Returns <c>null</c> when
    /// the type is not iterable.
    ///
    /// <para>Single point of truth for "what does this type yield when you
    /// for-loop over it" — keeps the parser, runtime and TIC adapter aligned
    /// without each having to enumerate kind cases independently.</para>
    /// </summary>
    public FunnyType? GetEnumerableElementTypeOrNull() => BaseType switch {
        BaseFunnyType.ArrayOf      => ((ArrayTypeSpecification)_payload).FunnyType,
        BaseFunnyType.List         => ((ListTypeSpecification)_payload).FunnyType,
        BaseFunnyType.MutableArray => ((MutableArrayTypeSpecification)_payload).FunnyType,
        BaseFunnyType.FixedArray   => ((FixedArrayTypeSpecification)_payload).FunnyType,
        BaseFunnyType.Set          => ((SetTypeSpecification)_payload).FunnyType,
        BaseFunnyType.Enumerable   => ((EnumerableTypeSpecification)_payload).FunnyType,
        BaseFunnyType.Map          => StructOf(
            ("key",   ((MapTypeSpecification)_payload).KeyType),
            ("value", ((MapTypeSpecification)_payload).ValueType)),
        _ => null,
    };

    internal static FunnyType StructOf(StructTypeSpecification fields) => new(fields);

    public static FunnyType StructOf(params (string, FunnyType)[] fields) => StructOf(isFrozen: false, fields);

    public static FunnyType StructOf(bool isFrozen, params (string, FunnyType)[] fields) {
        var specs = new StructTypeSpecification(fields.Length, isFrozen: isFrozen);
        foreach (var field in fields)
        {
            specs.Add(field.Item1, field.Item2);
        }
        return new(specs);
    }
    public static FunnyType FunOf(FunnyType returnType, params FunnyType[] inputTypes)
        => new(output: returnType, inputs: inputTypes);

    public static FunnyType Generic(int genericId) => new(genericId);

    public static FunnyType CustomOf(IFunnyCustomTypeDefinition definition) => new(definition);

    /// <summary>Named struct reference for recursive type definitions.</summary>
    internal static FunnyType NamedStructOf(string typeName) => new(typeName);

    // Tagged union: _payload holds the active specification based on BaseType
    //   ArrayOf   → ArrayTypeSpecification
    //   Optional  → OptionalTypeSpecification
    //   Struct    → StructTypeSpecification (IStructTypeSpecification)
    //   Fun       → FunTypeSpecification
    //   Custom    → IFunnyCustomTypeDefinition
    //   NamedStruct → string (type name)
    //   Generic   → null (_extra holds the generic id)
    //   Primitive → null
    private readonly object _payload;

    // Dual-purpose int:
    //   When BaseType == Generic: stores GenericId
    //   Otherwise: stores _genericArgumentsCount
    private readonly int _extra;

    private FunnyType(string namedStructTypeName) {
        BaseType = BaseFunnyType.NamedStruct;
        _payload = namedStructTypeName;
        _extra = 0;
    }

    private FunnyType(IFunnyCustomTypeDefinition definitionRuleViolation) {
        BaseType = BaseFunnyType.Custom;
        _payload = definitionRuleViolation;
        _extra = 0;
    }

    private FunnyType(FunnyType output, FunnyType[] inputs) {
        BaseType = BaseFunnyType.Fun;
        _payload = new FunTypeSpecification(output, inputs);
        _extra = inputs.Length + 1;
    }

    private FunnyType(int genericId) {
        BaseType = BaseFunnyType.Generic;
        _payload = null;
        _extra = genericId;
    }

    private FunnyType(BaseFunnyType baseType) {
        BaseType = baseType;
        _payload = null;
        _extra = 0;
    }

    private FunnyType(FunnyType arrayElementType) {
        BaseType = BaseFunnyType.ArrayOf;
        _payload = new ArrayTypeSpecification(arrayElementType);
        _extra = 1;
    }

    private FunnyType(FunnyType elementType, bool isOptional) {
        BaseType = BaseFunnyType.Optional;
        _payload = new OptionalTypeSpecification(elementType);
        _extra = 1;
    }

    private FunnyType(StructTypeSpecification fields) {
        BaseType = BaseFunnyType.Struct;
        _payload = fields;
        _extra = 0;
    }

    private FunnyType(ListTypeSpecification listSpec) {
        BaseType = BaseFunnyType.List;
        _payload = listSpec;
        _extra = 1;
    }

    private FunnyType(MutableArrayTypeSpecification mutArraySpec) {
        BaseType = BaseFunnyType.MutableArray;
        _payload = mutArraySpec;
        _extra = 1;
    }

    private FunnyType(FixedArrayTypeSpecification fixedArraySpec) {
        BaseType = BaseFunnyType.FixedArray;
        _payload = fixedArraySpec;
        _extra = 1;
    }

    private FunnyType(EnumerableTypeSpecification enumerableSpec) {
        BaseType = BaseFunnyType.Enumerable;
        _payload = enumerableSpec;
        _extra = 1;
    }

    private FunnyType(SetTypeSpecification setSpec) {
        BaseType = BaseFunnyType.Set;
        _payload = setSpec;
        _extra = 1;
    }

    private FunnyType(ClearableTypeSpecification mutableSpec) {
        BaseType = BaseFunnyType.Clearable;
        _payload = mutableSpec;
        _extra = 1;
    }

    private FunnyType(MapTypeSpecification mapSpec) {
        BaseType = BaseFunnyType.Map;
        _payload = mapSpec;
        _extra = 2;
    }

    public bool IsText => (BaseType == BaseFunnyType.ArrayOf && ((ArrayTypeSpecification)_payload).FunnyType.BaseType == BaseFunnyType.Char)
                       || (BaseType == BaseFunnyType.FixedArray && ((FixedArrayTypeSpecification)_payload).FunnyType.BaseType == BaseFunnyType.Char);

    /// <summary>True when this is a lang-mode <c>list&lt;T&gt;</c>.</summary>
    public bool IsList => BaseType == BaseFunnyType.List;

    /// <summary>True when this is a lang-mode mutable <c>array&lt;T&gt;</c>.</summary>
    public bool IsMutableArray => BaseType == BaseFunnyType.MutableArray;

    /// <summary>True when this is a lang-mode immutable <c>fixedArray&lt;T&gt;</c>.</summary>
    public bool IsFixedArray => BaseType == BaseFunnyType.FixedArray;

    /// <summary>True when this is a Stage C constraint-only <c>Enumerable&lt;T&gt;</c>.</summary>
    public bool IsEnumerable => BaseType == BaseFunnyType.Enumerable;

    /// <summary>True when this is a lang-mode unordered <c>set&lt;T&gt;</c>.</summary>
    public bool IsSet => BaseType == BaseFunnyType.Set;

    /// <summary>True when this is a Stage C constraint-only <c>Mutable&lt;T&gt;</c>.</summary>
    public bool IsMutableConstraint => BaseType == BaseFunnyType.Clearable;

    /// <summary>True when this is a lang-mode <c>map&lt;K, V&gt;</c>.</summary>
    public bool IsMap => BaseType == BaseFunnyType.Map;

    public readonly BaseFunnyType BaseType;

    public IStructTypeSpecification StructTypeSpecification => _payload as IStructTypeSpecification;

    public ArrayTypeSpecification ArrayTypeSpecification => _payload as ArrayTypeSpecification;

    public ListTypeSpecification ListTypeSpecification => _payload as ListTypeSpecification;

    public MutableArrayTypeSpecification MutableArrayTypeSpecification => _payload as MutableArrayTypeSpecification;

    public FixedArrayTypeSpecification FixedArrayTypeSpecification => _payload as FixedArrayTypeSpecification;

    public EnumerableTypeSpecification EnumerableTypeSpecification => _payload as EnumerableTypeSpecification;

    public SetTypeSpecification SetTypeSpecification => _payload as SetTypeSpecification;

    public ClearableTypeSpecification ClearableTypeSpecification => _payload as ClearableTypeSpecification;

    public MapTypeSpecification MapTypeSpecification => _payload as MapTypeSpecification;

    public OptionalTypeSpecification OptionalTypeSpecification => _payload as OptionalTypeSpecification;

    public FunTypeSpecification FunTypeSpecification => _payload as FunTypeSpecification;

    public IFunnyCustomTypeDefinition CustomTypeDefinition => _payload as IFunnyCustomTypeDefinition;
    public string NamedStructTypeName => _payload as string;

    /// <summary>
    /// Type arguments count
    /// 0 for primitives and structs
    /// 1 for arrays
    /// N+1 for functions with N arguments
    /// </summary>
    private int _genericArgumentsCount => BaseType == BaseFunnyType.Generic ? 0 : _extra;

    /// <summary>
    /// Returns type argument for complex types.
    /// For array - it returns element type if index == 0
    /// For function - it returns return type if index ==0 and i-th argument type if index == i +1
    ///
    /// Struct type contains no generic arguments as they are unsorted
    /// </summary>
    /// <exception cref="InvalidOperationException">Type is primitive or index is more than GenericArgumentsCount</exception>
    public FunnyType GetGenericArgument(int index) {
        if (IsPrimitive)
            throw new InvalidOperationException($"Type '{this}' contains no generic arguments because it is primitive");
        if (index == 0)
        {
            return BaseType switch {
                BaseFunnyType.ArrayOf  => ((ArrayTypeSpecification)_payload).FunnyType,
                BaseFunnyType.List     => ((ListTypeSpecification)_payload).FunnyType,
                BaseFunnyType.MutableArray => ((MutableArrayTypeSpecification)_payload).FunnyType,
                BaseFunnyType.FixedArray => ((FixedArrayTypeSpecification)_payload).FunnyType,
                BaseFunnyType.Enumerable => ((EnumerableTypeSpecification)_payload).FunnyType,
                BaseFunnyType.Set      => ((SetTypeSpecification)_payload).FunnyType,
                BaseFunnyType.Clearable  => ((ClearableTypeSpecification)_payload).FunnyType,
                BaseFunnyType.Map      => ((MapTypeSpecification)_payload).KeyType,
                BaseFunnyType.Optional => ((OptionalTypeSpecification)_payload).ElementType,
                BaseFunnyType.Fun      => ((FunTypeSpecification)_payload).Output,
                _                      => throw new InvalidOperationException($"Type '{this}' contains no generic arguments")
            };
        }
        if (index == 1 && BaseType == BaseFunnyType.Map)
            return ((MapTypeSpecification)_payload).ValueType;
        var count = _genericArgumentsCount;
        if (index >= count)
            throw new InvalidOperationException($"Type '{this}' contains only {count} generic arguments");
        return ((FunTypeSpecification)_payload).Inputs[index - 1];
    }

    /// <summary>
    /// In case of generic base type it shows index of generic variable
    /// </summary>
    internal int? GenericId => BaseType == BaseFunnyType.Generic ? _extra : null;

    public static bool operator ==(FunnyType obj1, FunnyType obj2)
        => obj1.Equals(obj2);

    // this is second one '!='
    public static bool operator !=(FunnyType obj1, FunnyType obj2)
        => !obj1.Equals(obj2);

    public bool IsPrimitive
        => (BaseType >= BaseFunnyType.Char && BaseType <= BaseFunnyType.Ip) || BaseType == BaseFunnyType.Int8 || BaseType == BaseFunnyType.Float32 || BaseType == BaseFunnyType.Any || BaseType == BaseFunnyType.None || BaseType == BaseFunnyType.Custom;

    public bool IsNumeric()
        => (BaseType >= BaseFunnyType.UInt8 && BaseType <= BaseFunnyType.Real)
           || BaseType == BaseFunnyType.Int8 || BaseType == BaseFunnyType.Float32;

    internal static readonly StringComparer StructKeyComparer = StringComparer.InvariantCultureIgnoreCase;

    /// <summary>
    /// Substitude concrete types to generic type definition (if it is)
    ///
    /// Example:
    /// generic:   Fun(T1, int)-> T0[];   solved: {int, text}
    /// returns:   Fun(text,int)-> int[];
    /// </summary>
    public static FunnyType SubstituteConcreteTypes(FunnyType genericOrNot, FunnyType[] solvedTypes) {
        switch (genericOrNot.BaseType)
        {
            case BaseFunnyType.Empty:
            case BaseFunnyType.Bool:
            case BaseFunnyType.Int8:
            case BaseFunnyType.Int16:
            case BaseFunnyType.Int32:
            case BaseFunnyType.Int64:
            case BaseFunnyType.UInt8:
            case BaseFunnyType.UInt16:
            case BaseFunnyType.UInt32:
            case BaseFunnyType.UInt64:
            case BaseFunnyType.Float32:
            case BaseFunnyType.Real:
            case BaseFunnyType.Char:
            case BaseFunnyType.Ip:
            case BaseFunnyType.Any:
            case BaseFunnyType.None:
            case BaseFunnyType.Custom:
                return genericOrNot;
            case BaseFunnyType.Optional:
                return OptionalOf(SubstituteConcreteTypes(((OptionalTypeSpecification)genericOrNot._payload).ElementType, solvedTypes));
            case BaseFunnyType.ArrayOf:
                return ArrayOf(SubstituteConcreteTypes(((ArrayTypeSpecification)genericOrNot._payload).FunnyType, solvedTypes));
            case BaseFunnyType.List:
                return ListOf(SubstituteConcreteTypes(((ListTypeSpecification)genericOrNot._payload).FunnyType, solvedTypes));
            case BaseFunnyType.MutableArray:
                return MutableArrayOf(SubstituteConcreteTypes(((MutableArrayTypeSpecification)genericOrNot._payload).FunnyType, solvedTypes));
            case BaseFunnyType.FixedArray:
                return FixedArrayOf(SubstituteConcreteTypes(((FixedArrayTypeSpecification)genericOrNot._payload).FunnyType, solvedTypes));
            case BaseFunnyType.Enumerable:
                // Element substitutes; container kind stays constraint-only (Enumerable).
                // The actual concrete kind at runtime is whatever satisfied the CompCS — the
                // runtime impl casts to IFunnyEnumerable, not to a specific concrete type.
                return EnumerableOf(SubstituteConcreteTypes(((EnumerableTypeSpecification)genericOrNot._payload).FunnyType, solvedTypes));
            case BaseFunnyType.Set:
                return SetOf(SubstituteConcreteTypes(((SetTypeSpecification)genericOrNot._payload).FunnyType, solvedTypes));
            case BaseFunnyType.Clearable:
                return ClearableOf(SubstituteConcreteTypes(((ClearableTypeSpecification)genericOrNot._payload).FunnyType, solvedTypes));
            case BaseFunnyType.Map: {
                var spec = (MapTypeSpecification)genericOrNot._payload;
                return MapOf(
                    SubstituteConcreteTypes(spec.KeyType, solvedTypes),
                    SubstituteConcreteTypes(spec.ValueType, solvedTypes));
            }
            case BaseFunnyType.Fun:
                var funSpec = (FunTypeSpecification)genericOrNot._payload;
                var outputTypes = new FunnyType[funSpec.Inputs.Length];
                for (int i = 0; i < funSpec.Inputs.Length; i++)
                    outputTypes[i] =
                        SubstituteConcreteTypes(funSpec.Inputs[i], solvedTypes);
                return FunOf(
                    SubstituteConcreteTypes(funSpec.Output, solvedTypes),
                    outputTypes);
            case BaseFunnyType.Generic:
                return solvedTypes[genericOrNot._extra];
            case BaseFunnyType.Struct: {
                // Struct with possibly-generic fields. Walk fields and
                // substitute each one. Preserves IsFrozen.
                var src = (StructTypeSpecification)genericOrNot._payload;
                var fields = new (string, FunnyType)[src.Count];
                int i = 0;
                foreach (var field in src)
                    fields[i++] = (field.Key, SubstituteConcreteTypes(field.Value, solvedTypes));
                return StructOf(src.IsFrozen, fields);
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static bool TrySolveGenericTypes(
        FunnyType[] genericArguments, FunnyType genericType, FunnyType concreteType, bool strict = false) {
        while (true)
        {
            switch (genericType.BaseType)
            {
                case BaseFunnyType.Generic:
                {
                    var id = genericType._extra;
                    if (genericArguments[id].BaseType == BaseFunnyType.Empty)
                    {
                        genericArguments[id] = concreteType;
                    }
                    else if (genericArguments[id] != concreteType)
                    {
                        if (genericArguments[id].CanBeConvertedTo(concreteType))
                        {
                            genericArguments[id] = concreteType;
                            return true;
                        }

                        if (strict) return false;

                        if (!concreteType.CanBeConvertedTo(genericArguments[id])) return false;
                    }

                    return true;
                }
                case BaseFunnyType.Optional when concreteType.BaseType != BaseFunnyType.Optional:
                    return false;
                case BaseFunnyType.Optional:
                    genericType = ((OptionalTypeSpecification)genericType._payload).ElementType;
                    concreteType = ((OptionalTypeSpecification)concreteType._payload).ElementType;
                    strict = false;
                    continue;
                case BaseFunnyType.ArrayOf when concreteType.BaseType != BaseFunnyType.ArrayOf:
                    return false;
                case BaseFunnyType.ArrayOf:
                    genericType = ((ArrayTypeSpecification)genericType._payload).FunnyType;
                    concreteType = ((ArrayTypeSpecification)concreteType._payload).FunnyType;
                    strict = false;
                    continue;
                case BaseFunnyType.List when concreteType.BaseType != BaseFunnyType.List:
                    return false;
                case BaseFunnyType.List:
                    // list<T> is INVARIANT in element — generic resolution requires equality,
                    // not subtype. Switch to strict mode for the recursive comparison.
                    genericType = ((ListTypeSpecification)genericType._payload).FunnyType;
                    concreteType = ((ListTypeSpecification)concreteType._payload).FunnyType;
                    strict = true;
                    continue;
                case BaseFunnyType.MutableArray when concreteType.BaseType != BaseFunnyType.MutableArray:
                    return false;
                case BaseFunnyType.MutableArray:
                    // array<T> is INVARIANT in element (same rule as list).
                    genericType = ((MutableArrayTypeSpecification)genericType._payload).FunnyType;
                    concreteType = ((MutableArrayTypeSpecification)concreteType._payload).FunnyType;
                    strict = true;
                    continue;
                case BaseFunnyType.FixedArray when concreteType.BaseType != BaseFunnyType.FixedArray:
                    return false;
                case BaseFunnyType.FixedArray:
                    // fixedArray<T> is INVARIANT in element (same rule as list/array).
                    genericType = ((FixedArrayTypeSpecification)genericType._payload).FunnyType;
                    concreteType = ((FixedArrayTypeSpecification)concreteType._payload).FunnyType;
                    strict = true;
                    continue;
                case BaseFunnyType.Set when concreteType.BaseType != BaseFunnyType.Set:
                    return false;
                case BaseFunnyType.Set:
                    // set<T> is INVARIANT in element (same rule as list/array/fixedArray).
                    genericType = ((SetTypeSpecification)genericType._payload).FunnyType;
                    concreteType = ((SetTypeSpecification)concreteType._payload).FunnyType;
                    strict = true;
                    continue;
                case BaseFunnyType.Map when concreteType.BaseType != BaseFunnyType.Map:
                    return false;
                case BaseFunnyType.Map: {
                    // map<K,V> is INVARIANT in both args. Recurse on K then V.
                    var gm = (MapTypeSpecification)genericType._payload;
                    var cm = (MapTypeSpecification)concreteType._payload;
                    if (!TrySolveGenericTypes(genericArguments, gm.KeyType, cm.KeyType, strict: true))
                        return false;
                    if (!TrySolveGenericTypes(genericArguments, gm.ValueType, cm.ValueType, strict: true))
                        return false;
                    return true;
                }
                case BaseFunnyType.Clearable:
                    // Mutable<V> accepts list / array / set / map kinds — every
                    // ConstructorKind for which ConstructorLattice.IsMutable is
                    // true. FixedArray / ArrayOf ee / Enumerable are NOT mutable
                    // — reject. Map's element is the synthesized {key, value}
                    // pair-struct (Specs/Tic/Algebra/StateMap.md §3), parallel
                    // to how Enumerable<T> sees Map.
                    {
                        FunnyType mutableElement;
                        switch (concreteType.BaseType) {
                            case BaseFunnyType.Clearable:
                                mutableElement = ((ClearableTypeSpecification)concreteType._payload).FunnyType;
                                break;
                            case BaseFunnyType.List:
                                mutableElement = ((ListTypeSpecification)concreteType._payload).FunnyType;
                                break;
                            case BaseFunnyType.MutableArray:
                                mutableElement = ((MutableArrayTypeSpecification)concreteType._payload).FunnyType;
                                break;
                            case BaseFunnyType.Set:
                                mutableElement = ((SetTypeSpecification)concreteType._payload).FunnyType;
                                break;
                            case BaseFunnyType.Map:
                                {
                                    var mapSpec = (MapTypeSpecification)concreteType._payload;
                                    mutableElement = FunnyType.StructOf(
                                        ("key", mapSpec.KeyType),
                                        ("value", mapSpec.ValueType));
                                    break;
                                }
                            default:
                                return false; // not a mutable container
                        }
                        genericType = ((ClearableTypeSpecification)genericType._payload).FunnyType;
                        concreteType = mutableElement;
                        strict = true;
                        continue;
                    }
                case BaseFunnyType.Enumerable:
                    // Enumerable<V> accepts any concrete collection kind whose element fits V.
                    // Extract concrete element by container kind; element is invariant (V identity).
                    {
                        FunnyType concreteElement;
                        switch (concreteType.BaseType) {
                            case BaseFunnyType.Enumerable:
                                concreteElement = ((EnumerableTypeSpecification)concreteType._payload).FunnyType;
                                break;
                            case BaseFunnyType.ArrayOf:
                                concreteElement = ((ArrayTypeSpecification)concreteType._payload).FunnyType;
                                break;
                            case BaseFunnyType.List:
                                concreteElement = ((ListTypeSpecification)concreteType._payload).FunnyType;
                                break;
                            case BaseFunnyType.MutableArray:
                                concreteElement = ((MutableArrayTypeSpecification)concreteType._payload).FunnyType;
                                break;
                            case BaseFunnyType.FixedArray:
                                concreteElement = ((FixedArrayTypeSpecification)concreteType._payload).FunnyType;
                                break;
                            case BaseFunnyType.Set:
                                concreteElement = ((SetTypeSpecification)concreteType._payload).FunnyType;
                                break;
                            default:
                                return false; // not a collection — Enumerable not satisfiable
                        }
                        genericType = ((EnumerableTypeSpecification)genericType._payload).FunnyType;
                        concreteType = concreteElement;
                        strict = true; // element invariance
                        continue;
                    }
                case BaseFunnyType.Fun when concreteType.BaseType != BaseFunnyType.Fun:
                    return false;
                case BaseFunnyType.Fun:
                {
                    var genericFun = (FunTypeSpecification)genericType._payload;
                    var concreteFun = (FunTypeSpecification)concreteType._payload;

                    if (!TrySolveGenericTypes(genericArguments, genericFun.Output, concreteFun.Output))
                        return false;
                    if (concreteFun.Inputs.Length != genericFun.Inputs.Length)
                        return false;
                    for (int i = 0; i < concreteFun.Inputs.Length; i++)
                    {
                        if (!TrySolveGenericTypes(genericArguments, genericFun.Inputs[i], concreteFun.Inputs[i]))
                            return false;
                    }

                    return true;
                }
                default:
                    return concreteType.CanBeConvertedTo(genericType);
            }
        }
    }

    public int? SearchMaxGenericTypeId() {
        switch (BaseType)
        {
            case BaseFunnyType.Bool:
            case BaseFunnyType.Int8:
            case BaseFunnyType.Int16:
            case BaseFunnyType.Int32:
            case BaseFunnyType.Int64:
            case BaseFunnyType.UInt8:
            case BaseFunnyType.UInt16:
            case BaseFunnyType.UInt32:
            case BaseFunnyType.UInt64:
            case BaseFunnyType.Float32:
            case BaseFunnyType.Real:
            case BaseFunnyType.Char:
            case BaseFunnyType.Ip:
            case BaseFunnyType.Any:
            case BaseFunnyType.None:
            case BaseFunnyType.Custom:
                return null;
            case BaseFunnyType.Optional:
                return ((OptionalTypeSpecification)_payload).ElementType.SearchMaxGenericTypeId();
            case BaseFunnyType.ArrayOf:
                return ((ArrayTypeSpecification)_payload).FunnyType.SearchMaxGenericTypeId();
            case BaseFunnyType.List:
                return ((ListTypeSpecification)_payload).FunnyType.SearchMaxGenericTypeId();
            case BaseFunnyType.MutableArray:
                return ((MutableArrayTypeSpecification)_payload).FunnyType.SearchMaxGenericTypeId();
            case BaseFunnyType.FixedArray:
                return ((FixedArrayTypeSpecification)_payload).FunnyType.SearchMaxGenericTypeId();
            case BaseFunnyType.Enumerable:
                return ((EnumerableTypeSpecification)_payload).FunnyType.SearchMaxGenericTypeId();
            case BaseFunnyType.Set:
                return ((SetTypeSpecification)_payload).FunnyType.SearchMaxGenericTypeId();
            case BaseFunnyType.Clearable:
                return ((ClearableTypeSpecification)_payload).FunnyType.SearchMaxGenericTypeId();
            case BaseFunnyType.Map: {
                var spec = (MapTypeSpecification)_payload;
                var kId = spec.KeyType.SearchMaxGenericTypeId();
                var vId = spec.ValueType.SearchMaxGenericTypeId();
                if (!kId.HasValue) return vId;
                if (!vId.HasValue) return kId;
                return Math.Max(kId.Value, vId.Value);
            }
            case BaseFunnyType.Fun:
                var funSpec = (FunTypeSpecification)_payload;
                var iId = funSpec.Inputs.Select(i => i.SearchMaxGenericTypeId()).Max();
                var oId = funSpec.Output.SearchMaxGenericTypeId();
                if (!iId.HasValue) return oId;
                if (!oId.HasValue) return iId;
                return Math.Max(iId.Value, oId.Value);
            case BaseFunnyType.Struct:
                return ((IStructTypeSpecification)_payload).Values.Select(i => i.SearchMaxGenericTypeId()).Max();
            case BaseFunnyType.NamedStruct:
                return null;
            case BaseFunnyType.Generic:
                return _extra;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override string ToString() =>
        BaseType switch {
            BaseFunnyType.ArrayOf  => ((ArrayTypeSpecification)_payload).FunnyType + "[]",
            BaseFunnyType.List     => "list<" + ((ListTypeSpecification)_payload).FunnyType + ">",
            BaseFunnyType.MutableArray => "array<" + ((MutableArrayTypeSpecification)_payload).FunnyType + ">",
            BaseFunnyType.FixedArray => "fixedArray<" + ((FixedArrayTypeSpecification)_payload).FunnyType + ">",
            BaseFunnyType.Enumerable => "enumerable<" + ((EnumerableTypeSpecification)_payload).FunnyType + ">",
            BaseFunnyType.Set      => "set<" + ((SetTypeSpecification)_payload).FunnyType + ">",
            BaseFunnyType.Clearable  => "clearable<" + ((ClearableTypeSpecification)_payload).FunnyType + ">",
            BaseFunnyType.Map      => "map<" + ((MapTypeSpecification)_payload).KeyType + "," + ((MapTypeSpecification)_payload).ValueType + ">",
            BaseFunnyType.Optional => ((OptionalTypeSpecification)_payload).ElementType + "?",
            // Internal "None" type — surfaced when a slot holds only `none`
            // (e.g. `x = none`). User-facing convention: render as `any?` so
            // it matches the user-facing Optional surface. Bug hunt round 3 SC-4.
            BaseFunnyType.None     => "any?",
            BaseFunnyType.Fun      => $"({string.Join(",", ((FunTypeSpecification)_payload).Inputs)})->{((FunTypeSpecification)_payload).Output}",
            BaseFunnyType.Struct =>
                $"{{{string.Join(";", ((IStructTypeSpecification)_payload).Select(s => s.Key + ":" + s.Value))}}}",
            BaseFunnyType.Generic => "T" + _extra,
            BaseFunnyType.Custom  => ((IFunnyCustomTypeDefinition)_payload)?.Name ?? "custom",
            BaseFunnyType.NamedStruct => (string)_payload ?? "named?",
            _                     => BaseType.ToString()
        };

    // ReSharper disable once MemberCanBePrivate.Global
    public bool CanBeConvertedTo(FunnyType to) =>
        VarTypeConverter.CanBeConverted(this, to);

    public override bool Equals(object obj) =>
        obj is FunnyType other && Equals(other);

    public bool Equals(FunnyType obj) =>
        BaseType == obj.BaseType
        && Equals(_payload, obj._payload)
        && _extra == obj._extra;

    public override int GetHashCode() {
        unchecked
        {
            var hashCode = (int)BaseType;
            hashCode = (hashCode * 397) ^ (_payload != null ? _payload.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ _extra;
            return hashCode;
        }
    }
}
