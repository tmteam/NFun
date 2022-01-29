using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.TypeInferenceAdapter {

public abstract class TicTypesConverter {
    public static readonly TicTypesConverter Concrete
        = new OnlyConcreteTypesConverter();

    public static TicTypesConverter GenericSignatureConverter(ConstrainsState[] constrainsMap)
        => new ConstrainsConverter(constrainsMap);

    public static TicTypesConverter ReplaceGenericTypesConverter(
        ConstrainsState[] constrainsMap, IList<FunnyType> genericArgs)
        => new GenericMapConverter(constrainsMap, genericArgs);

    public abstract FunnyType Convert(ITicNodeState type);

    class OnlyConcreteTypesConverter : TicTypesConverter {
        public override FunnyType Convert(ITicNodeState type) {
            while (true)
            {
                switch (type)
                {
                    case StateRefTo refTo:
                        type = refTo.Element;
                        continue;
                    case StatePrimitive primitive:
                        return ToConcrete(primitive.Name);
                    case ConstrainsState constrains when constrains.Preferred != null:
                        return ToConcrete(constrains.Preferred.Name);
                    case ConstrainsState constrains when !constrains.HasAncestor:
                    {
                        if (constrains.IsComparable) return FunnyType.Real;
                        return FunnyType.Any;
                    }
                    case ConstrainsState constrains:
                    {
                        if (constrains.Ancestor.Name.HasFlag(PrimitiveTypeName._isAbstract))
                        {
                            switch (constrains.Ancestor.Name)
                            {
                                case PrimitiveTypeName.I96:
                                {
                                    if (constrains.HasDescendant &&
                                        constrains.Descendant.CanBeImplicitlyConvertedTo(StatePrimitive.I32))
                                        return FunnyType.Int32;
                                    return FunnyType.Int64;
                                }
                                case PrimitiveTypeName.I48:
                                {
                                    if (constrains.HasDescendant &&
                                        constrains.Descendant.CanBeImplicitlyConvertedTo(StatePrimitive.I32))
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
                        return FunnyType.ArrayOf(Convert(array.Element));
                    case StateFun fun:
                        return FunnyType.FunOf(
                            Convert(fun.ReturnType),
                            fun.ArgNodes.SelectToArray(a => Convert(a.State)));
                    case StateStruct str:
                        return FunnyType.StructOf(
                            str.Fields.ToDictionary(
                                f => f.Key.ToLower(),
                                f => Convert(f.Value.State),
                                FunnyType.StructKeyComparer));
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }

    private class ConstrainsConverter : TicTypesConverter {
        private readonly ConstrainsState[] _constrainsMap;

        public ConstrainsConverter(ConstrainsState[] constrainsMap) => _constrainsMap = constrainsMap;

        public override FunnyType Convert(ITicNodeState type) =>
            type switch {
                StateRefTo refTo           => Convert(refTo.Element),
                StatePrimitive primitive   => ToConcrete(primitive.Name),
                ConstrainsState constrains => FunnyType.Generic(GetGenericIndexOrThrow(constrains)),
                StateArray array           => FunnyType.ArrayOf(Convert(array.Element)),
                StateFun fun => FunnyType.FunOf(
                    Convert(fun.ReturnType),
                    fun.ArgNodes.SelectToArray(a => Convert(a.State))),
                StateStruct @struct => FunnyType.StructOf(
                    @struct.Fields.ToDictionary(
                        keySelector: f => f.Key.ToLower(),
                        elementSelector: f => Convert(f.Value.GetNonReference().State),
                        FunnyType.StructKeyComparer)),
                _ => throw new NotSupportedException($"State {type} is not supported for convertion to Fun type")
            };

        private int GetGenericIndexOrThrow(ConstrainsState constrains) {
            var index = System.Array.IndexOf(_constrainsMap, constrains);
            if (index == -1)
                throw new InvalidOperationException("Unknown constrains");
            return index;
        }
    }

    private class GenericMapConverter : TicTypesConverter {
        private readonly ConstrainsState[] _constrainsMap;
        private readonly IList<FunnyType> _argTypes;

        public GenericMapConverter(ConstrainsState[] constrainsMap, IList<FunnyType> argTypes) {
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
                    case StatePrimitive primitive:
                        return ToConcrete(primitive.Name);
                    //case Constrains constrains when constrains.Preferred != null:
                    //    return ToConcrete(constrains.Preferred.Name);
                    case ConstrainsState constrains:
                        var index = System.Array.IndexOf(_constrainsMap, constrains);
                        if (index == -1) throw new InvalidOperationException("Unknown constrains");
                        return _argTypes[index];
                    case StateArray array:
                        return FunnyType.ArrayOf(Convert(array.Element));
                    case StateFun fun:
                        return FunnyType.FunOf(
                            Convert(fun.ReturnType),
                            fun.ArgNodes.SelectToArray(a => Convert(a.State)));
                    case StateStruct @struct:
                        return FunnyType.StructOf(
                            @struct.Fields.ToDictionary(
                                f => f.Key.ToLower(),
                                f => Convert(f.Value.State),
                                FunnyType.StructKeyComparer));
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }

    public static FunnyType ToConcrete(PrimitiveTypeName name) =>
        name switch {
            PrimitiveTypeName.Any  => FunnyType.Any,
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
            PrimitiveTypeName.I48  => FunnyType.Int32,
            PrimitiveTypeName.U48 =>
                throw new InvalidOperationException("Cannot cast abstract type " + name),
            PrimitiveTypeName.U24 =>
                throw new InvalidOperationException("Cannot cast abstract type " + name),
            PrimitiveTypeName.U12 =>
                throw new InvalidOperationException("Cannot cast abstract type " + name),
            _ => throw new ArgumentOutOfRangeException()
        };
}

}