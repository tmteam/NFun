using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Types;

namespace NFun; 

/// <summary>
/// NFun type description
/// </summary>
public readonly struct FunnyType {

    internal static readonly FunnyType Empty = new(); 
    public static readonly FunnyType Any    = new(BaseFunnyType.Any);
    public static readonly FunnyType Bool   = new(BaseFunnyType.Bool);
    public static readonly FunnyType Char   = new(BaseFunnyType.Char);
    public static readonly FunnyType UInt8  = new(BaseFunnyType.UInt8);
    public static readonly FunnyType UInt16 = new(BaseFunnyType.UInt16);
    public static readonly FunnyType UInt32 = new(BaseFunnyType.UInt32);
    public static readonly FunnyType UInt64 = new(BaseFunnyType.UInt64);
    public static readonly FunnyType Int16  = new(BaseFunnyType.Int16);
    public static readonly FunnyType Int32  = new(BaseFunnyType.Int32);
    public static readonly FunnyType Int64  = new(BaseFunnyType.Int64);
    public static readonly FunnyType Real   = new(BaseFunnyType.Real);
    public static readonly FunnyType Text   = ArrayOf(Char);

    public static FunnyType PrimitiveOf(BaseFunnyType baseType) => new(baseType);
    
    public static FunnyType ArrayOf(FunnyType type) => new(type);

    internal static FunnyType StructOf(StructTypeSpecification fields) => new(fields);
    
    public static FunnyType StructOf(params (string, FunnyType)[] fields) {
        var specs = new StructTypeSpecification(fields.Length);
        foreach (var field in fields)
        {
            specs.Add(field.Item1, field.Item2);
        }
        return new(specs);
    }

    public static FunnyType FunOf(FunnyType returnType, params FunnyType[] inputTypes)
        => new(output: returnType, inputs: inputTypes);

    public static FunnyType Generic(int genericId) => new(genericId);

    private FunnyType(FunnyType output, FunnyType[] inputs) {
        FunTypeSpecification = new FunTypeSpecification(output, inputs);
        BaseType = BaseFunnyType.Fun;
        ArrayTypeSpecification = null;
        GenericId = null;
        StructTypeSpecification = null;
        _genericArgumentsCount = inputs.Length + 1;
    }

    private FunnyType(int genericId) {
        BaseType = BaseFunnyType.Generic;
        FunTypeSpecification = null;
        ArrayTypeSpecification = null;
        StructTypeSpecification = null;
        GenericId = genericId;
        _genericArgumentsCount = 0;
    }

    private FunnyType(BaseFunnyType baseType) {
        BaseType = baseType;
        StructTypeSpecification = null;

        FunTypeSpecification = null;
        ArrayTypeSpecification = null;
        GenericId = null;
        _genericArgumentsCount = 0;
    }

    private FunnyType(FunnyType arrayElementType) {
        BaseType = BaseFunnyType.ArrayOf;
        StructTypeSpecification = null;
        FunTypeSpecification = null;
        ArrayTypeSpecification = new ArrayTypeSpecification(arrayElementType);
        GenericId = null;
        _genericArgumentsCount = 1;
    }

    private FunnyType(StructTypeSpecification fields) {
        BaseType = BaseFunnyType.Struct;
        StructTypeSpecification = fields;
        FunTypeSpecification = null;
        ArrayTypeSpecification = null;
        GenericId = null;
        _genericArgumentsCount = 0;
    }

    public bool IsText => ArrayTypeSpecification?.FunnyType.BaseType == BaseFunnyType.Char;

    public readonly BaseFunnyType BaseType;

    public readonly IReadOnlyDictionary<string, FunnyType> StructTypeSpecification;

    internal readonly ArrayTypeSpecification ArrayTypeSpecification;

    internal readonly FunTypeSpecification FunTypeSpecification;

    /// <summary>
    /// Type arguments count
    /// 0 for primitives and structs
    /// 1 for arrays
    /// N+1 for functions with N arguments
    /// </summary>
    private readonly int _genericArgumentsCount;
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
            return ArrayTypeSpecification?.FunnyType ?? FunTypeSpecification.Output;
        if (index >= _genericArgumentsCount)
            throw new InvalidOperationException($"Type '{this}' contains only {_genericArgumentsCount} generic arguments");
        return FunTypeSpecification.Inputs[index - 1];
    }

    /// <summary>
    /// In case of generic base type it shows index of generic variable 
    /// </summary>
    internal readonly int? GenericId;

    public static bool operator ==(FunnyType obj1, FunnyType obj2)
        => obj1.Equals(obj2);

    // this is second one '!='
    public static bool operator !=(FunnyType obj1, FunnyType obj2)
        => !obj1.Equals(obj2);

    public bool IsPrimitive
        => (BaseType >= BaseFunnyType.Char && BaseType <= BaseFunnyType.Real) || BaseType == BaseFunnyType.Any;

    public bool IsNumeric()
        => BaseType >= BaseFunnyType.UInt8 && BaseType <= BaseFunnyType.Real;

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
            case BaseFunnyType.Int16:
            case BaseFunnyType.Int32:
            case BaseFunnyType.Int64:
            case BaseFunnyType.UInt8:
            case BaseFunnyType.UInt16:
            case BaseFunnyType.UInt32:
            case BaseFunnyType.UInt64:
            case BaseFunnyType.Real:
            case BaseFunnyType.Char:
            case BaseFunnyType.Any:
                return genericOrNot;
            case BaseFunnyType.ArrayOf:
                return ArrayOf(SubstituteConcreteTypes(genericOrNot.ArrayTypeSpecification.FunnyType, solvedTypes));
            case BaseFunnyType.Fun:
                var outputTypes = new FunnyType[genericOrNot.FunTypeSpecification.Inputs.Length];
                for (int i = 0; i < genericOrNot.FunTypeSpecification.Inputs.Length; i++)
                    outputTypes[i] =
                        SubstituteConcreteTypes(genericOrNot.FunTypeSpecification.Inputs[i], solvedTypes);
                return FunOf(
                    SubstituteConcreteTypes(genericOrNot.FunTypeSpecification.Output, solvedTypes),
                    outputTypes);
            case BaseFunnyType.Generic:
                return solvedTypes[genericOrNot.GenericId.Value];
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
                    var id = genericType.GenericId.Value;
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
                case BaseFunnyType.ArrayOf when concreteType.BaseType != BaseFunnyType.ArrayOf:
                    return false;
                case BaseFunnyType.ArrayOf:
                    genericType = genericType.ArrayTypeSpecification.FunnyType;
                    concreteType = concreteType.ArrayTypeSpecification.FunnyType;
                    strict = false;
                    continue;
                case BaseFunnyType.Fun when concreteType.BaseType != BaseFunnyType.Fun:
                    return false;
                case BaseFunnyType.Fun:
                {
                    var genericFun = genericType.FunTypeSpecification;
                    var concreteFun = concreteType.FunTypeSpecification;

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
            case BaseFunnyType.Int16:
            case BaseFunnyType.Int32:
            case BaseFunnyType.Int64:
            case BaseFunnyType.UInt8:
            case BaseFunnyType.UInt16:
            case BaseFunnyType.UInt32:
            case BaseFunnyType.UInt64:
            case BaseFunnyType.Real:
            case BaseFunnyType.Char:
            case BaseFunnyType.Any:
                return null;
            case BaseFunnyType.ArrayOf:
                return ArrayTypeSpecification.FunnyType.SearchMaxGenericTypeId();
            case BaseFunnyType.Fun:
                var iId = FunTypeSpecification.Inputs.Select(i => i.SearchMaxGenericTypeId()).Max();
                var oId = FunTypeSpecification.Output.SearchMaxGenericTypeId();
                if (!iId.HasValue) return oId;
                if (!oId.HasValue) return iId;
                return Math.Max(iId.Value, oId.Value);
            case BaseFunnyType.Struct:
                return StructTypeSpecification.Values.Select(i => i.SearchMaxGenericTypeId()).Max();
            case BaseFunnyType.Generic:
                return GenericId;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override string ToString() =>
        BaseType switch {
            BaseFunnyType.ArrayOf => ArrayTypeSpecification.FunnyType + "[]",
            BaseFunnyType.Fun     => $"({string.Join(",", FunTypeSpecification.Inputs)})->{FunTypeSpecification.Output}",
            BaseFunnyType.Struct =>
                $"{{{string.Join(";", StructTypeSpecification.Select(s => s.Key + ":" + s.Value))}}}",
            BaseFunnyType.Generic => "T_" + GenericId,
            _                     => BaseType.ToString()
        };

    // ReSharper disable once MemberCanBePrivate.Global
    public bool CanBeConvertedTo(FunnyType to) => 
        VarTypeConverter.CanBeConverted(this, to);
    
    public override bool Equals(object obj) => 
        obj is FunnyType other && Equals(other);

    public bool Equals(FunnyType obj) => 
        BaseType == obj.BaseType 
        && Equals(StructTypeSpecification, obj.StructTypeSpecification) 
        && Equals(ArrayTypeSpecification, obj.ArrayTypeSpecification) 
        && Equals(FunTypeSpecification, obj.FunTypeSpecification) 
        && _genericArgumentsCount == obj._genericArgumentsCount 
        && GenericId == obj.GenericId;

    public override int GetHashCode() {
        unchecked
        {
            var hashCode = (int)BaseType;
            hashCode = (hashCode * 397) ^ (StructTypeSpecification != null ? StructTypeSpecification.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ArrayTypeSpecification != null ? ArrayTypeSpecification.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (FunTypeSpecification != null ? FunTypeSpecification.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ _genericArgumentsCount;
            hashCode = (hashCode * 397) ^ GenericId.GetHashCode();
            return hashCode;
        }
    }
}