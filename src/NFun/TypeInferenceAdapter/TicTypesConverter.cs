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

        public static TicTypesConverter ReplaceGenericTypesConverter(ConstrainsState[] constrainsMap, IList<VarType> genericArgs)
            => new GenericMapConverter(constrainsMap, genericArgs);

        public abstract VarType Convert(ITicNodeState type);

        class OnlyConcreteTypesConverter : TicTypesConverter
        {
            public override VarType Convert(ITicNodeState type)
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
                                return VarType.Real;
                            return VarType.Anything;
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
                                                return VarType.Int32;
                                            return VarType.Int64;
                                        }
                                    case PrimitiveTypeName.I48:
                                        {
                                            if (constrains.HasDescendant || constrains.Descedant.CanBeImplicitlyConvertedTo(StatePrimitive.I32))
                                                return VarType.Int32;
                                            return VarType.UInt32;
                                        }
                                    case PrimitiveTypeName.U48: return VarType.UInt32;
                                    case PrimitiveTypeName.U24: return VarType.UInt16;
                                    case PrimitiveTypeName.U12: return VarType.UInt8;
                                    default: throw new NotSupportedException();
                                }
                            }
                            return ToConcrete(constrains.Ancestor.Name);
                        }

                    case StateArray array:
                        return VarType.ArrayOf(Convert(array.Element));
                    case StateFun fun:
                        return VarType.Fun(Convert(fun.ReturnType), fun.ArgNodes.SelectToArray(a=>Convert(a.State)));
                    case StateStruct str:
                        return VarType.StructOf(str.Fields.ToDictionary(f => f.Key, f => Convert(f.Value.State)));
                    default:
                        throw new NotSupportedException();
                }
            }

           
        }

        private class ConstrainsConverter : TicTypesConverter
        {
            private readonly ConstrainsState[] _constrainsMap;

            public ConstrainsConverter(ConstrainsState[] constrainsMap) => _constrainsMap = constrainsMap;

            public override VarType Convert(ITicNodeState type) =>
                type switch
                {
                    StateRefTo refTo => Convert(refTo.Element),
                    StatePrimitive primitive => ToConcrete(primitive.Name),
                    ConstrainsState constrains => VarType.Generic(GetGenericIndexOrThrow(constrains)),
                    StateArray array => VarType.ArrayOf(Convert(array.Element)),
                    StateFun fun => VarType.Fun(Convert(fun.ReturnType),
                        fun.ArgNodes.SelectToArray(a => Convert(a.State))),
                    StateStruct strct => VarType.StructOf(strct.Fields.ToDictionary(
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
            private readonly IList<VarType> _argTypes;

            public GenericMapConverter(ConstrainsState[] constrainsMap, IList<VarType> argTypes)
            {
                _constrainsMap = constrainsMap;
                _argTypes = argTypes;
            }

            public override VarType Convert(ITicNodeState type)
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
                        return VarType.ArrayOf(Convert(array.Element));
                    case StateFun fun:
                        return VarType.Fun(Convert(fun.ReturnType), fun.ArgNodes.SelectToArray(a=>Convert(a.State)));
                    case StateStruct @struct:
                        return VarType.StructOf(@struct.Fields.ToDictionary(
                            f => f.Key,
                            f => Convert(f.Value.State)));
                    default:
                        throw new NotSupportedException();
                }
            }
        }
        public static VarType ToConcrete(PrimitiveTypeName name)
        {
            switch (name)
            {
                case PrimitiveTypeName.Any: return VarType.Anything;
                case PrimitiveTypeName.Char: return VarType.Char;
                case PrimitiveTypeName.Bool: return VarType.Bool;
                case PrimitiveTypeName.Real: return VarType.Real;
                case PrimitiveTypeName.I64: return VarType.Int64;
                case PrimitiveTypeName.I32: return VarType.Int32;
                case PrimitiveTypeName.I24: return VarType.Int32;
                case PrimitiveTypeName.I16: return VarType.Int16;
                case PrimitiveTypeName.U64: return VarType.UInt64;
                case PrimitiveTypeName.U32: return VarType.UInt32;
                case PrimitiveTypeName.U16: return VarType.UInt16;
                case PrimitiveTypeName.U8: return VarType.UInt8;

                case PrimitiveTypeName.I96: return VarType.Int64; /*return VarType.Real;*/
                case PrimitiveTypeName.I48: return VarType.Int32;/*;*/
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