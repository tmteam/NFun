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

    }
}