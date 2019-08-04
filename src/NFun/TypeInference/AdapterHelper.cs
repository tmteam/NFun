using System;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.TypeInference.Solving;
using NFun.Types;

namespace NFun.HindleyMilner
{
    public static class AdapterHelper
    {
        public static string GetArgAlias(string funAlias, string argId)
            =>  funAlias + "::" + argId;
        public static string GetFunAlias(string funId, int argsCount)
            =>  funId + "(" + argsCount+")";

        public static string GetFunAlias(this UserFunctionDefenitionSyntaxNode syntaxNode)
            => GetFunAlias(syntaxNode.Id, syntaxNode.Args.Count);

        public static TiType GetHmFunctionalType(this FunctionBase functionBase)
        {
            return TiType.Fun(
                functionBase.ReturnType.ConvertToHmType(),
                functionBase.ArgTypes.Select(a => a.ConvertToHmType()).ToArray());
        }
        
        public static TiType ConvertToHmType(this VarType origin)
        {
            switch (origin.BaseType)
            {
                case BaseVarType.Bool:  return TiType.Bool;
                case BaseVarType.Int16: return TiType.Int16;
                case BaseVarType.Int32: return TiType.Int32;
                case BaseVarType.Int64: return TiType.Int64;
                case BaseVarType.UInt8:  return TiType.UInt8;
                case BaseVarType.UInt16: return TiType.UInt16;
                case BaseVarType.UInt32: return TiType.UInt32;
                case BaseVarType.UInt64: return TiType.UInt64;
                case BaseVarType.Real:  return TiType.Real;
                case BaseVarType.Char:  return TiType.Char;
                case BaseVarType.Any:   return TiType.Any;
                case BaseVarType.ArrayOf: return TiType.ArrayOf(ConvertToHmType(origin.ArrayTypeSpecification.VarType));
                case BaseVarType.Generic: return TiType.Generic(origin.GenericId.Value);
                case BaseVarType.Fun: 
                    return TiType.Fun(ConvertToHmType( origin.FunTypeSpecification.Output),
                    origin.FunTypeSpecification.Inputs.Select(ConvertToHmType).ToArray());
                case BaseVarType.Empty: 
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}