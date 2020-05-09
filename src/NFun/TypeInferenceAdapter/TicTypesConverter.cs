using System;
using System.Linq;
using NFun.Tic.SolvingStates;
using NFun.Types;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.TypeInferenceAdapter
{
    public abstract class  TicTypesConverter
    {
        public static readonly TicTypesConverter Concrete 
            = new OnlyConcreteTypesConverter();

        public static TicTypesConverter GenericSignatureConverter(Constrains[] constrainsMap) 
            => new ConstrainsConverter(constrainsMap);

        public static TicTypesConverter ReplaceGenericTypesConverter(Constrains[] constrainsMap,VarType[] genericArgs)
            => new GenericMapConverter(constrainsMap, genericArgs);

        public abstract VarType Convert(IState type);


        class OnlyConcreteTypesConverter : TicTypesConverter
        {
            public override VarType Convert(IState type)
            {
                switch (type)
                {
                    case RefTo refTo:
                        return Convert(refTo.Element);
                    case Primitive primitive:
                        return ToConcrete(primitive.Name);
                    case Constrains constrains when constrains.Prefered != null:
                        return ToConcrete(constrains.Prefered.Name);
                    case Constrains constrains when !constrains.HasAncestor:
                        {
                            if (constrains.IsComparable)
                                throw new NotImplementedException();
                            return VarType.Anything;
                        }

                    case Constrains constrains:
                        {
                            if (constrains.Ancestor.Name.HasFlag(PrimitiveTypeName._isAbstract))
                            {
                                switch (constrains.Ancestor.Name)
                                {
                                    case PrimitiveTypeName.I96:
                                        {
                                            if (constrains.HasDescendant || constrains.Descedant.CanBeImplicitlyConvertedTo(Primitive.I32))
                                                return VarType.Int32;
                                            return VarType.Int64;
                                        }
                                    case PrimitiveTypeName.I48:
                                        {
                                            if (constrains.HasDescendant || constrains.Descedant.CanBeImplicitlyConvertedTo(Primitive.I32))
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

                    case Array array:
                        return VarType.ArrayOf(Convert(array.Element));
                    case Fun fun:
                        return VarType.Fun(Convert(fun.ReturnType), fun.Args.Select(Convert).ToArray());
                    default:
                        throw new NotSupportedException();
                }
            }

           
        }

        class ConstrainsConverter : TicTypesConverter
        {
            private readonly Constrains[] _constrainsMap;

            public ConstrainsConverter(Constrains[] constrainsMap) => _constrainsMap = constrainsMap;

            public override VarType Convert(IState type)
            {
                switch (type)
                {
                    case RefTo refTo: 
                        return Convert(refTo.Element);
                    case Primitive primitive:
                        return ToConcrete(primitive.Name);
                    case Constrains constrains:
                        var index = System.Array.IndexOf(_constrainsMap,constrains);
                        if(index==-1)
                            throw new InvalidOperationException("Unknown constrains");
                        return VarType.Generic(index);
                    case Array array:
                        return VarType.ArrayOf(Convert(array.Element));
                    case Fun fun:
                        return VarType.Fun(Convert(fun.ReturnType), fun.Args.Select(Convert).ToArray());
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        class GenericMapConverter : TicTypesConverter
        {
            private readonly Constrains[] _constrainsMap;
            private readonly VarType[] _argTypes;

            public GenericMapConverter(Constrains[] constrainsMap, VarType[] argTypes)
            {
                _constrainsMap = constrainsMap;
                _argTypes = argTypes;
            }

            public override VarType Convert(IState type)
            {
                switch (type)
                {
                    case RefTo refTo:
                        return Convert(refTo.Element);
                    case Primitive primitive:
                        return ToConcrete(primitive.Name);
                    //case Constrains constrains when constrains.Prefered != null:
                    //    return ToConcrete(constrains.Prefered.Name);
                    case Constrains constrains:
                        var index = System.Array.IndexOf(_constrainsMap, constrains);
                        if (index == -1)
                            throw new InvalidOperationException("Unknown constrains");
                        return _argTypes[index];
                    case Array array:
                        return VarType.ArrayOf(Convert(array.Element));
                    case Fun fun:
                        return VarType.Fun(Convert(fun.ReturnType), fun.Args.Select(Convert).ToArray());
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