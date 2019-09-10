using System;
using System.Linq;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.TypeInference.Solving;
using NFun.Types;

namespace NFun.TypeInference
{
    public static class LangTiHelper
    {
        /// <summary>
        /// Setups ti algorithm
        /// </summary>
        /// <returns>null if setup failed, Algorithm solver otherwise</returns>
        public static TiLanguageSolver SetupTiOrNull(ISyntaxNode syntaxNode, FunctionsDictionary dictionary,
            SetupTiState state = null)
        {
            var solver = state?.CurrentSolver??new TiLanguageSolver();
            var visitorState = state??new SetupTiState(solver);
            var enterVisitor = new SetupTiEnterVisitor(visitorState);
            var exitVisitor = new SetupTiExitVisitor(visitorState, dictionary);
            if (syntaxNode.ComeOver(enterVisitor, exitVisitor)) 
                return solver;
            return null;
        }
        public static string GetArgAlias(string funAlias, string argId)
            =>  funAlias + "::" + argId;
        public static string GetFunAlias(string funId, int argsCount)
            =>  funId + "(" + argsCount+")";

        public static string GetFunAlias(this UserFunctionDefenitionSyntaxNode syntaxNode)
            => GetFunAlias(syntaxNode.Id, syntaxNode.Args.Count);

        public static TiType GetHmFunctionalType(this FunctionBase functionBase)
        {
            return TiType.Fun(
                functionBase.ReturnType.ConvertToTiType(),
                functionBase.ArgTypes.Select(a => a.ConvertToTiType()).ToArray());
        }
        
        public static TiType ConvertToTiType(this VarType origin)
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
                case BaseVarType.ArrayOf: return TiType.ArrayOf(ConvertToTiType(origin.ArrayTypeSpecification.VarType));
                case BaseVarType.Generic: return TiType.Generic(origin.GenericId.Value);
                case BaseVarType.Fun: 
                    return TiType.Fun(ConvertToTiType( origin.FunTypeSpecification.Output),
                    origin.FunTypeSpecification.Inputs.Select(ConvertToTiType).ToArray());
                case BaseVarType.Empty: 
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public static VarType GetVarType(this TiResult result, string varId, TiToLangTypeConverter converter)
            => converter.ToSimpleType( result.GetVarType(varId));
        public static LangFunctionSignature GetFunctionOverload(this TiResult result, int nodeId,TiToLangTypeConverter converter)
        {
            var overloadHmSignature = result.GetFunctionOverload(nodeId);
            if (overloadHmSignature == null)
                return null;
            return new LangFunctionSignature(
                converter.ToSimpleType(overloadHmSignature.ReturnType), 
                overloadHmSignature.ArgTypes.Select(o=>converter.ToSimpleType(o)).ToArray());
        }
        
        public static VarType GetNodeTypeOrEmpty(this TiResult result, int nodeId, TiToLangTypeConverter converter)
        {
            var hmType = result.GetNodeTypeOrNull(nodeId);
            if (hmType == null)
                return VarType.Empty;
            return converter.ToSimpleType(hmType);
        }
    }
}