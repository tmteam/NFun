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
    public static readonly FunnyType Real   = new(BaseFunnyType.Real);
    public static readonly FunnyType Text   = ArrayOf(Char);
    public static readonly FunnyType None   = new(BaseFunnyType.None);

    public static FunnyType PrimitiveOf(BaseFunnyType baseType) => new(baseType);

    public static FunnyType ArrayOf(FunnyType type) => new(type);

    public static FunnyType OptionalOf(FunnyType type) =>
        type.BaseType == BaseFunnyType.Optional ? type : new(type, isOptional: true);

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

    public bool IsText => BaseType == BaseFunnyType.ArrayOf && ((ArrayTypeSpecification)_payload).FunnyType.BaseType == BaseFunnyType.Char;

    public readonly BaseFunnyType BaseType;

    public IStructTypeSpecification StructTypeSpecification => _payload as IStructTypeSpecification;

    public ArrayTypeSpecification ArrayTypeSpecification => _payload as ArrayTypeSpecification;

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
                BaseFunnyType.Optional => ((OptionalTypeSpecification)_payload).ElementType,
                BaseFunnyType.Fun      => ((FunTypeSpecification)_payload).Output,
                _                      => throw new InvalidOperationException($"Type '{this}' contains no generic arguments")
            };
        }
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
        => (BaseType >= BaseFunnyType.Char && BaseType <= BaseFunnyType.Ip) || BaseType == BaseFunnyType.Int8 || BaseType == BaseFunnyType.Any || BaseType == BaseFunnyType.None || BaseType == BaseFunnyType.Custom;

    public bool IsNumeric()
        => (BaseType >= BaseFunnyType.UInt8 && BaseType <= BaseFunnyType.Real)
           || BaseType == BaseFunnyType.Int8;

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
            BaseFunnyType.Optional => ((OptionalTypeSpecification)_payload).ElementType + "?",
            BaseFunnyType.None     => "none",
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
