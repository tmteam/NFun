using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.TypeInferenceAdapter;

public abstract class TicTypesConverter {
    public static readonly TicTypesConverter Concrete = new OnlyConcreteTypesConverter();

    public static TicTypesConverter GenericSignatureConverter(IReadOnlyList<ConstraintsState> constrainsMap)
        => new ConstrainsConverter(constrainsMap);

    public static TicTypesConverter ReplaceGenericTypesConverter(
        IReadOnlyList<ConstraintsState> constrainsMap, IList<FunnyType> genericArgs)
        => new GenericMapConverter(constrainsMap, genericArgs);

    public abstract FunnyType Convert(ITicNodeState type);
    /// <summary>Per-conversion unique mark. Set once per ConvertToFunnyStruct entry.</summary>
    private int _convertMark;
    /// <summary>Named struct types currently being converted (for self-referential cycle detection).</summary>
    private HashSet<string> _convertingNamedTypes;

    private FunnyType ConvertToFunnyStruct(StateStruct str) {
        if (_convertMark == 0) _convertMark = Tic.SolvingFunctions.NextMark();

        // Struct-level cycle detection for self-referential named types.
        // When TIC solving fills a recursion boundary with the enclosing named struct,
        // field nodes are shared between levels. Per-node marks can miss this cycle,
        // so we also track by TypeName.
        if (str.TypeName != null) {
            _convertingNamedTypes ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!_convertingNamedTypes.Add(str.TypeName))
                return FunnyType.NamedStructOf(str.TypeName);
        }

        var fields = new StructTypeSpecification(str.FieldsCount, isFrozen: str.IsFrozen);
        foreach (var ticField in str.Fields)
        {
            var fieldNode = ticField.Value.GetNonReference();
            if (fieldNode.VisitMark == _convertMark)
            {
                // Cycle detected — use TypeName if available (from node state or parent chain)
                if (fieldNode.State is Tic.SolvingStates.StateStruct { TypeName: { } tn })
                    fields.Add(ticField.Key.ToLower(), FunnyType.NamedStructOf(tn));
                // If this is a StateStruct (TypeName lost via GetNonReferenced)
                // and we're inside a named struct conversion, use the parent's TypeName.
                else if (fieldNode.State is Tic.SolvingStates.StateStruct
                         && _convertingNamedTypes is { Count: > 0 })
                    fields.Add(ticField.Key.ToLower(),
                        FunnyType.NamedStructOf(_convertingNamedTypes.First()));
                else
                    fields.Add(ticField.Key.ToLower(), FunnyType.Any);
                continue;
            }
            var prev = fieldNode.VisitMark;
            fieldNode.VisitMark = _convertMark;
            fields.Add(ticField.Key.ToLower(), Convert(fieldNode.State));
            fieldNode.VisitMark = prev;
        }

        if (str.TypeName != null)
            _convertingNamedTypes!.Remove(str.TypeName);

        return FunnyType.StructOf(fields);
    }

    private FunnyType ConvertToFunnyFun(StateFun fun)
        => FunnyType.FunOf(Convert(fun.ReturnType), fun.ArgNodes.SelectToArray(a => Convert(a.State)));

    private FunnyType ConvertToFunnyArray(StateArray array)
        => FunnyType.ArrayOf(Convert(array.Element));

    private const int OptionalConvertMark = -58000;
    private FunnyType ConvertToFunnyOptional(StateOptional opt) {
        // Cycle guard: generic functions with if..else none create cyclic Optionals
        var elem = opt.ElementNode;
        if (elem.VisitMark == OptionalConvertMark)
            return FunnyType.Any; // break cycle
        var prev = elem.VisitMark;
        elem.VisitMark = OptionalConvertMark;
        var result = FunnyType.OptionalOf(Convert(opt.Element));
        elem.VisitMark = prev;
        return result;
    }

    class OnlyConcreteTypesConverter : TicTypesConverter {
        public OnlyConcreteTypesConverter() { }
        public override FunnyType Convert(ITicNodeState type) {
            while (true)
            {
                switch (type)
                {
                    case StateRefTo refTo:
                        type = refTo.Element;
                        continue;
                    case StatePrimitiveCustom custom:
                        return custom.OriginalFunnyType;
                    case StatePrimitive { Name: PrimitiveTypeName.Any }
                        when _convertingNamedTypes is { Count: > 0 }:
                        // Inside a named struct conversion, Any at the recursion boundary
                        // should be NamedStructOf so the call site matches by named type
                        // rather than trying to merge struct with Any.
                        return FunnyType.NamedStructOf(_convertingNamedTypes.First());
                    case StatePrimitive primitive:
                        return ToConcrete(primitive.Name);
                    case ConstraintsState constrains when constrains.Preferred != null:
                        if (constrains.HasDescendant && constrains.Descendant is StateOptional)
                            return FunnyType.OptionalOf(ToConcrete(constrains.Preferred.Name));
                        return ToConcrete(constrains.Preferred.Name);
                    case ConstraintsState constrains when !constrains.HasAncestor:
                    {
                        if (constrains.IsComparable) return FunnyType.Real;
                        // Inside a named struct: Empty constraint = recursion boundary
                        if (_convertingNamedTypes is { Count: > 0 } && constrains.NoConstrains)
                            return FunnyType.NamedStructOf(_convertingNamedTypes.First());
                        return FunnyType.Any;
                    }
                    case ConstraintsState constrains:
                    {
                        if (constrains.Ancestor.Name.HasFlag(PrimitiveTypeName._isAbstract))
                        {
                            switch (constrains.Ancestor.Name)
                            {
                                case PrimitiveTypeName.I96:
                                {
                                    if (constrains.HasDescendant &&
                                        constrains.Descendant.CanBePessimisticConvertedTo(StatePrimitive.I32))
                                        return FunnyType.Int32;
                                    return FunnyType.Int64;
                                }
                                case PrimitiveTypeName.I48:
                                {
                                    if (constrains.HasDescendant &&
                                        constrains.Descendant.CanBePessimisticConvertedTo(StatePrimitive.I32))
                                        return FunnyType.Int32;
                                    return FunnyType.UInt32;
                                }
                                case PrimitiveTypeName.U48:
                                    return FunnyType.UInt32;
                                case PrimitiveTypeName.U24:
                                    return FunnyType.UInt16;
                                case PrimitiveTypeName.U12:
                                    return FunnyType.UInt8;
                                default:
                                    throw new NotSupportedException();
                            }
                        }

                        return ToConcrete(constrains.Ancestor.Name);
                    }

                    case StateArray array:
                        return ConvertToFunnyArray(array);
                    case StateOptional opt:
                        return ConvertToFunnyOptional(opt);
                    case StateFun fun:
                        return ConvertToFunnyFun(fun);
                    case StateStruct str:
                        return ConvertToFunnyStruct(str);
                    default:
                        throw new NFunImpossibleException($"Type {type?.ToString()??"<null>"} is not supported for convertion");
                }
            }
        }
    }

    private class ConstrainsConverter : TicTypesConverter {
        private readonly IReadOnlyList<ConstraintsState> _constrainsMap;

        public ConstrainsConverter(IReadOnlyList<ConstraintsState> constrainsMap) => _constrainsMap = constrainsMap;

        public override FunnyType Convert(ITicNodeState type)
            => type switch {
                   StateRefTo refTo           => Convert(refTo.Element),
                   StatePrimitiveCustom custom         => custom.OriginalFunnyType,
                   StatePrimitive primitive   => ToConcrete(primitive.Name),
                   ConstraintsState constrains => FunnyType.Generic(GetGenericIndexOrThrow(constrains)),
                   StateArray array           => ConvertToFunnyArray(array),
                   StateOptional opt          => ConvertToFunnyOptional(opt),
                   StateFun fun               => ConvertToFunnyFun(fun),
                   StateStruct str            => ConvertToFunnyStruct(str),
                   _                          => throw new NotSupportedException($"State {type} is not supported for convertion to Fun type")
               };

        private int GetGenericIndexOrThrow(ConstraintsState constraints) {
            var index = _constrainsMap.IndexOf(constraints);
            if (index == -1)
                throw new InvalidOperationException("Unknown constraints");
            return index;
        }
    }

    private class GenericMapConverter : TicTypesConverter {
        private readonly IReadOnlyList<ConstraintsState> _constrainsMap;
        private readonly IList<FunnyType> _argTypes;

        public GenericMapConverter(IReadOnlyList<ConstraintsState> constrainsMap, IList<FunnyType> argTypes) {
            _constrainsMap = constrainsMap;
            _argTypes = argTypes;
        }

        public override FunnyType Convert(ITicNodeState type) {
            while (true)
            {
                switch (type)
                {
                    case StateRefTo refTo:
                        type = refTo.Element;
                        continue;
                    case StatePrimitiveCustom custom:
                        return custom.OriginalFunnyType;
                    case StatePrimitive primitive:
                        return ToConcrete(primitive.Name);
                    case ConstraintsState constrains:
                        var index = _constrainsMap.IndexOf(constrains);
                        if (index == -1) throw new InvalidOperationException("Unknown constrains");
                        return _argTypes[index];
                    case StateArray array:
                        return ConvertToFunnyArray(array);
                    case StateOptional opt:
                        return ConvertToFunnyOptional(opt);
                    case StateFun fun:
                        return ConvertToFunnyFun(fun);
                    case StateStruct str:
                        return ConvertToFunnyStruct(str);
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }

    public static FunnyType ToConcrete(PrimitiveTypeName name) =>
        name switch {
            PrimitiveTypeName.Any  => FunnyType.Any,
            PrimitiveTypeName.Ip   => FunnyType.Ip,
            PrimitiveTypeName.Char => FunnyType.Char,
            PrimitiveTypeName.Bool => FunnyType.Bool,
            PrimitiveTypeName.Real => FunnyType.Real,
            PrimitiveTypeName.I64  => FunnyType.Int64,
            PrimitiveTypeName.I32  => FunnyType.Int32,
            PrimitiveTypeName.I24  => FunnyType.Int32,
            PrimitiveTypeName.I16  => FunnyType.Int16,
            PrimitiveTypeName.U64  => FunnyType.UInt64,
            PrimitiveTypeName.U32  => FunnyType.UInt32,
            PrimitiveTypeName.U16  => FunnyType.UInt16,
            PrimitiveTypeName.U8   => FunnyType.UInt8,
            PrimitiveTypeName.I96  => FunnyType.Int64,
            PrimitiveTypeName.I48  => FunnyType.Int64,
            // Abstract types can appear as bare StatePrimitive when MergeOrNull collapses
            // a constraint interval to a single point (ancestor == descendant).
            // Map each to its nearest concrete ancestor that fits all values.
            PrimitiveTypeName.U48  => FunnyType.UInt64,
            PrimitiveTypeName.U24  => FunnyType.UInt32,
            PrimitiveTypeName.U12  => FunnyType.UInt16,
            PrimitiveTypeName.None => FunnyType.None,
            _ => throw new ArgumentOutOfRangeException()
        };
}
