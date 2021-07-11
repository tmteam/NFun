using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.TypeInferenceAdapter
{
    public abstract class  TicTypesConverter
    {
        public static readonly TicTypesConverter Concrete 
            = new OnlyConcreteTypesConverter();

        public static TicTypesConverter GenericSignatureConverter(ConstrainsState[] constrainsMap) 
            => new ConstrainsConverter(constrainsMap);

        public static TicTypesConverter ReplaceGenericTypesConverter(ConstrainsState[] constrainsMap, IList<FunnyType> genericArgs)
            => new GenericMapConverter(constrainsMap, genericArgs);

        public abstract FunnyType Convert(ITicNodeState type);

        class OnlyConcreteTypesConverter : TicTypesConverter
        {
            public override FunnyType Convert(ITicNodeState type)
            {
                switch (type)
                {
                    case StateRefTo refTo:
                        return Convert(refTo.Element);
                    case StatePrimitive primitive:
                        return ToConcrete(primitive.Name);
                    case ConstrainsState constrains when constrains.Prefered != null:
                        return ToConcrete(constrains.Prefered.Name);
                    case ConstrainsState constrains when !constrains.HasAncestor:
                        {
                            if (constrains.IsComparable)
                                return FunnyType.Real;
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
                                            if (constrains.HasDescendant || constrains.Descedant.CanBeImplicitlyConvertedTo(StatePrimitive.I32))
                                                return FunnyType.Int32;
                                            return FunnyType.Int64;
                                        }
                                    case PrimitiveTypeName.I48:
                                        {
                                            if (constrains.HasDescendant || constrains.Descedant.CanBeImplicitlyConvertedTo(StatePrimitive.I32))
                                                return FunnyType.Int32;
                                            return FunnyType.UInt32;
                                        }
                                    case PrimitiveTypeName.U48: return FunnyType.UInt32;
                                    case PrimitiveTypeName.U24: return FunnyType.UInt16;
                                    case PrimitiveTypeName.U12: return FunnyType.UInt8;
                                    default: throw new NotSupportedException();
                                }
                            }
                            return ToConcrete(constrains.Ancestor.Name);
                        }

                    case StateArray array:
                        return FunnyType.ArrayOf(Convert(array.Element));
                    case StateFun fun:
                        return FunnyType.Fun(Convert(fun.ReturnType), fun.ArgNodes.SelectToArray(a=>Convert(a.State)));
                    case StateStruct str:
                        return FunnyType.StructOf(str.Fields.ToDictionary(f => f.Key, f => Convert(f.Value.State)));
                    default:
                        throw new NotSupportedException();
                }
            }

           
        }

        private class ConstrainsConverter : TicTypesConverter
        {
            private readonly ConstrainsState[] _constrainsMap;

            public ConstrainsConverter(ConstrainsState[] constrainsMap) => _constrainsMap = constrainsMap;

            public override FunnyType Convert(ITicNodeState type) =>
                type switch
                {
                    StateRefTo refTo => Convert(refTo.Element),
                    StatePrimitive primitive => ToConcrete(primitive.Name),
                    ConstrainsState constrains => FunnyType.Generic(GetGenericIndexOrThrow(constrains)),
                    StateArray array => FunnyType.ArrayOf(Convert(array.Element)),
                    StateFun fun => FunnyType.Fun(Convert(fun.ReturnType),
                        fun.ArgNodes.SelectToArray(a => Convert(a.State))),
                    StateStruct strct => FunnyType.StructOf(strct.Fields.ToDictionary(
                        keySelector:     f => f.Key,
                        elementSelector: f => Convert(f.Value.GetNonReference().State))),
                    _ => throw new NotSupportedException($"State {type} is not supported for convertion to Fun type")
                };

            private int GetGenericIndexOrThrow(ConstrainsState constrains)
            {
                var index = System.Array.IndexOf(_constrainsMap, constrains);
                if (index == -1)
                    throw new InvalidOperationException("Unknown constrains");
                return index;
            }
        }

        private class GenericMapConverter : TicTypesConverter
        {
            private readonly ConstrainsState[] _constrainsMap;
            private readonly IList<FunnyType> _argTypes;

            public GenericMapConverter(ConstrainsState[] constrainsMap, IList<FunnyType> argTypes)
            {
                _constrainsMap = constrainsMap;
                _argTypes = argTypes;
            }

            public override FunnyType Convert(ITicNodeState type)
            {
                switch (type)
                {
                    case StateRefTo refTo:
                        return Convert(refTo.Element);
                    case StatePrimitive primitive:
                        return ToConcrete(primitive.Name);
                    //case Constrains constrains when constrains.Prefered != null:
                    //    return ToConcrete(constrains.Prefered.Name);
                    case ConstrainsState constrains:
                        var index = System.Array.IndexOf(_constrainsMap, constrains);
                        if (index == -1)
                            throw new InvalidOperationException("Unknown constrains");
                        return _argTypes[index];
                    case StateArray array:
                        return FunnyType.ArrayOf(Convert(array.Element));
                    case StateFun fun:
                        return FunnyType.Fun(Convert(fun.ReturnType), fun.ArgNodes.SelectToArray(a=>Convert(a.State)));
                    case StateStruct @struct:
                        return FunnyType.StructOf(@struct.Fields.ToDictionary(
                            f => f.Key,
                            f => Convert(f.Value.State)));
                    default:
                        throw new NotSupportedException();
                }
            }
        }
        public static FunnyType ToConcrete(PrimitiveTypeName name)
        {
            switch (name)
            {
                case PrimitiveTypeName.Any: return FunnyType.Any;
                case PrimitiveTypeName.Char: return FunnyType.Char;
                case PrimitiveTypeName.Bool: return FunnyType.Bool;
                case PrimitiveTypeName.Real: return FunnyType.Real;
                case PrimitiveTypeName.I64: return FunnyType.Int64;
                case PrimitiveTypeName.I32: return FunnyType.Int32;
                case PrimitiveTypeName.I24: return FunnyType.Int32;
                case PrimitiveTypeName.I16: return FunnyType.Int16;
                case PrimitiveTypeName.U64: return FunnyType.UInt64;
                case PrimitiveTypeName.U32: return FunnyType.UInt32;
                case PrimitiveTypeName.U16: return FunnyType.UInt16;
                case PrimitiveTypeName.U8: return FunnyType.UInt8;

                case PrimitiveTypeName.I96: return FunnyType.Int64; /*return VarType.Real;*/
                case PrimitiveTypeName.I48: return FunnyType.Int32;/*;*/
                case PrimitiveTypeName.U48: /*return VarType.Int64;*/
                case PrimitiveTypeName.U24: /*return VarType.Int32;*/
                case PrimitiveTypeName.U12: /*return VarType.Int16;*/
                    throw new InvalidOperationException("Cannot cast abstract type " + name);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}