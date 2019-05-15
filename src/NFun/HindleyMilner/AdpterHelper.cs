using System;
using System.Linq;
using NFun.HindleyMilner.Tyso;
using NFun.Parsing;
using NFun.Types;

namespace NFun.HindleyMilner
{
    public static class AdpterHelper
    {
        public static string GetArgAlias(string funAlias, string argId)
            =>  funAlias + "::" + argId;
        public static string GetFunAlias(string funId, int argsCount)
            =>  funId + "(" + argsCount+")";

        public static string GetFunAlias(this UserFunctionDefenitionSyntaxNode syntaxNode)
            => GetFunAlias(syntaxNode.Id, syntaxNode.Args.Count);
        public static FType ConvertToHmType(this VarType origin)
        {
            switch (origin.BaseType)
            {
                case BaseVarType.Bool:  return FType.Bool;
                case BaseVarType.Int32: return FType.Int32;
                case BaseVarType.Int64: return FType.Int64;
                case BaseVarType.Real:  return FType.Real;
                case BaseVarType.Text:  return FType.Text;
                case BaseVarType.Any:   return FType.Any;
                case BaseVarType.ArrayOf: return FType.ArrayOf(ConvertToHmType(origin.ArrayTypeSpecification.VarType));
                case BaseVarType.Generic: return FType.Generic(origin.GenericId.Value);
                case BaseVarType.Fun: 
                    return FType.Fun(ConvertToHmType( origin.FunTypeSpecification.Output),
                    origin.FunTypeSpecification.Inputs.Select(ConvertToHmType).ToArray());
                case BaseVarType.Empty: 
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static VarType ConvertToSimpleTypes(FType type)
        {
            if (type.IsPrimitiveGeneric)
                return VarType.Generic(((GenericType) type).GenericId);
            switch (type.Name.Id)
            {
                case NTypeName.AnyId: return VarType.Anything;
                case NTypeName.RealId: return VarType.Real;
                case NTypeName.TextId: return VarType.Text;
                case NTypeName.BoolId: return VarType.Bool;
                case NTypeName.Int64Id: return  VarType.Int64;
                case NTypeName.Int32Id: return VarType.Int32;
                case NTypeName.SomeIntegerId: return VarType.Int32;
                case NTypeName.ArrayId: return VarType.ArrayOf(ConvertToSimpleType(type.Arguments[0]));
                case NTypeName.FunId :
                    return VarType.Fun(ConvertToSimpleType(type.Arguments[0]), 
                        type.Arguments.Skip(1).Select(ConvertToSimpleType).ToArray()
                        );
            }
            throw new InvalidOperationException("Not supported type "+ type.ToSmartString(SolvingNode.MaxTypeDepth));
            
        }

        private static VarType ConvertToSimpleType(SolvingNode node) 
            => ConvertToSimpleTypes(node.MakeType(SolvingNode.MaxTypeDepth));
    }
}