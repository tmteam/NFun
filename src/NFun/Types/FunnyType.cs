using System;
using System.Collections.Generic;
using System.Linq;

namespace NFun.Types {

public readonly struct FunnyType {
    public override int GetHashCode() {
        unchecked
        {
            return ((int)BaseType * 397) ^
                   (ArrayTypeSpecification?.GetHashCode() ??
                    FunTypeSpecification?.GetHashCode() ?? StructTypeSpecification?.GetHashCode() ?? 0);
        }
    }

    public static FunnyType Empty => new();
    public static FunnyType PrimitiveOf(BaseFunnyType baseType) => new(baseType);
    public static FunnyType Any => new(BaseFunnyType.Any);
    public static FunnyType Bool => new(BaseFunnyType.Bool);
    public static FunnyType Char => new(BaseFunnyType.Char);

    public static FunnyType UInt8 => new(BaseFunnyType.UInt8);
    public static FunnyType UInt16 => new(BaseFunnyType.UInt16);
    public static FunnyType UInt32 => new(BaseFunnyType.UInt32);
    public static FunnyType UInt64 => new(BaseFunnyType.UInt64);

    public static FunnyType Int16 => new(BaseFunnyType.Int16);
    public static FunnyType Int32 => new(BaseFunnyType.Int32);
    public static FunnyType Int64 => new(BaseFunnyType.Int64);
    public static FunnyType Real => new(BaseFunnyType.Real);
    public static FunnyType Text => ArrayOf(Char);

    public static FunnyType StructOf(Dictionary<string, FunnyType> fields) {
        if (fields.Comparer != StructKeyComparer)
            throw new InvalidOperationException("Only FunnyType.StructKeyComparer comparator is allowed");
        return new FunnyType(fields);
    }

    public static FunnyType StructOf(params (string, FunnyType)[] fields)
        => new(fields.ToDictionary(f => f.Item1, f => f.Item2, StructKeyComparer));

    public static FunnyType StructOf(string fieldName, FunnyType fieldType)
        => new(new Dictionary<string, FunnyType>(StructKeyComparer) { { fieldName, fieldType } });


    public static FunnyType ArrayOf(FunnyType type) => new(type);

    public static FunnyType Fun(FunnyType returnType, params FunnyType[] inputTypes)
        => new(output: returnType, inputs: inputTypes);

    public static FunnyType Generic(int genericId) => new(genericId);
    internal static readonly StringComparer StructKeyComparer = StringComparer.InvariantCultureIgnoreCase;

    private FunnyType(FunnyType output, FunnyType[] inputs) {
        FunTypeSpecification = new FunTypeSpecification(output, inputs);
        BaseType = BaseFunnyType.Fun;
        ArrayTypeSpecification = null;
        GenericId = null;
        StructTypeSpecification = null;
    }

    private FunnyType(int genericId) {
        BaseType = BaseFunnyType.Generic;
        FunTypeSpecification = null;
        ArrayTypeSpecification = null;
        StructTypeSpecification = null;
        GenericId = genericId;
    }

    private FunnyType(BaseFunnyType baseType) {
        BaseType = baseType;
        StructTypeSpecification = null;

        FunTypeSpecification = null;
        ArrayTypeSpecification = null;
        GenericId = null;
    }

    private FunnyType(FunnyType arrayElementType) {
        BaseType = BaseFunnyType.ArrayOf;
        StructTypeSpecification = null;
        FunTypeSpecification = null;
        ArrayTypeSpecification = new AdditionalTypeSpecification(arrayElementType);
        GenericId = null;
    }

    private FunnyType(Dictionary<string, FunnyType> arrayElementType) {
        BaseType = BaseFunnyType.Struct;
        StructTypeSpecification = arrayElementType;
        FunTypeSpecification = null;
        ArrayTypeSpecification = null;
        GenericId = null;
    }

    public bool IsText => BaseType == BaseFunnyType.ArrayOf &&
                          ArrayTypeSpecification.FunnyType.BaseType == BaseFunnyType.Char;
    public readonly BaseFunnyType BaseType;
    public readonly Dictionary<string, FunnyType> StructTypeSpecification;
    public readonly AdditionalTypeSpecification ArrayTypeSpecification;
    public readonly FunTypeSpecification FunTypeSpecification;
    public readonly int? GenericId;

    public static bool operator ==(FunnyType obj1, FunnyType obj2)
        => obj1.Equals(obj2);

    // this is second one '!='
    public static bool operator !=(FunnyType obj1, FunnyType obj2)
        => !obj1.Equals(obj2);

    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) return false;
        return obj is FunnyType other && Equals(other);
    }

    // this is third one 'Equals'
    private bool Equals(FunnyType obj) {
        if (obj.BaseType != BaseType)
            return false;

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
                return true;
            case BaseFunnyType.ArrayOf:
                return ArrayTypeSpecification.FunnyType.Equals(obj.ArrayTypeSpecification.FunnyType);
            case BaseFunnyType.Fun:
            {
                var funA = FunTypeSpecification;
                var funB = obj.FunTypeSpecification;

                if (!funA.Output.Equals(funB.Output))
                    return false;

                for (int i = 0; i < funA.Inputs.Length; i++)
                {
                    if (!funA.Inputs[i].Equals(funB.Inputs[i]))
                        return false;
                }

                return true;
            }
            case BaseFunnyType.Generic:
                return GenericId == obj.GenericId;
            case BaseFunnyType.Struct:
                foreach (var (key, value) in StructTypeSpecification)
                {
                    if (!obj.StructTypeSpecification.TryGetValue(key, out var otherValue))
                        return false;
                    if (!value.Equals(otherValue))
                        return false;
                }

                return StructTypeSpecification.Count == obj.StructTypeSpecification.Count;
            default:
                return true;
        }
    }

    public bool IsPrimitive
        => (BaseType >= BaseFunnyType.Char && BaseType <= BaseFunnyType.Real) || BaseType == BaseFunnyType.Any;

    public bool IsNumeric()
        => BaseType >= BaseFunnyType.UInt8 && BaseType <= BaseFunnyType.Real;

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
                return Fun(
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
            BaseFunnyType.Fun => $"({string.Join(",", FunTypeSpecification.Inputs)})->{FunTypeSpecification.Output}",
            BaseFunnyType.Struct =>
                $"{{{string.Join(";", StructTypeSpecification.Select(s => s.Key + ":" + s.Value))}}}",
            BaseFunnyType.Generic => "T_" + GenericId,
            _                     => BaseType.ToString()
        };

    public bool CanBeConvertedTo(FunnyType to)
        => VarTypeConverter.CanBeConverted(this, to);
}

}