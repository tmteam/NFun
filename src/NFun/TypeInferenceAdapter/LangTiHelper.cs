using System;
using System.Linq;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.TypeInferenceAdapter
{
    public static class LangTiHelper
    {
        /// <summary>
        /// Setups ti algorithm
        /// </summary>
        /// <returns>null if setup failed, Algorithm solver otherwise</returns>
        public static GraphBuilder SetupTiOrNull(ISyntaxNode syntaxNode, FunctionDictionary dictionary, TypeInferenceResultsBuilder resultsBuilder,
            SetupTiState state = null)
        {
            var solver = state?.CurrentSolver??new GraphBuilder();
            var tiState = state??new SetupTiState(solver);
            var enterVisitor = new SetupTiEnterVisitor(tiState, dictionary, resultsBuilder);
            var exitVisitor = new SetupTiExitVisitor(tiState,   dictionary, resultsBuilder);
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

        public static IState GetTicFunType(this IFunctionSignature functionBase)
        {
            return Fun.Of(
                functionBase.ArgTypes.Select(a => a.ConvertToTiType()).ToArray(),
                functionBase.ReturnType.ConvertToTiType());
        }
        public static IState GetTicFunType(this GenericFunctionBase functionBase, RefTo[] genericMap) =>
            Fun.Of(
                functionBase.ArgTypes.Select(a => a.ConvertToTiType(genericMap)).ToArray(),
                functionBase.ReturnType.ConvertToTiType(genericMap));

        public static Primitive ConvertToTiType(this BaseVarType baseVarType)
        {
            switch (baseVarType)
            {
                case BaseVarType.Empty:  return null;
                case BaseVarType.Char:   return Primitive.Char;
                case BaseVarType.Bool:   return Primitive.Bool;
                case BaseVarType.UInt8:  return Primitive.U8;
                case BaseVarType.UInt16: return Primitive.U16;
                case BaseVarType.UInt32: return Primitive.U32;
                case BaseVarType.UInt64: return Primitive.U32;
                case BaseVarType.Int16:  return Primitive.I16;
                case BaseVarType.Int32:  return Primitive.I32;
                case BaseVarType.Int64:  return Primitive.I64;
                case BaseVarType.Real:   return Primitive.Real;
                case BaseVarType.Any:    return Primitive.Any;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baseVarType), baseVarType, null);
            }
        }
        public static IState ConvertToTiType(this VarType origin)
        {
            switch (origin.BaseType)
            {
                case BaseVarType.Bool:   return Primitive.Bool;
                case BaseVarType.Int16:  return Primitive.I16;
                case BaseVarType.Int32:  return Primitive.I32;
                case BaseVarType.Int64:  return Primitive.I64;
                case BaseVarType.UInt8:  return Primitive.U8;
                case BaseVarType.UInt16: return Primitive.U16;
                case BaseVarType.UInt32: return Primitive.U32;
                case BaseVarType.UInt64: return Primitive.U64;
                case BaseVarType.Real:   return Primitive.Real;
                case BaseVarType.Char:   return Primitive.Char;
                case BaseVarType.Any:    return Primitive.Any;
                case BaseVarType.ArrayOf: return Tic.SolvingStates.Array.Of(ConvertToTiType(origin.ArrayTypeSpecification.VarType));
                case BaseVarType.Fun: 
                    return Fun.Of(
                        argTypes: origin.FunTypeSpecification.Inputs.Select(ConvertToTiType).ToArray(), 
                        returnType: ConvertToTiType(origin.FunTypeSpecification.Output));
                case BaseVarType.Generic: 
                    throw new InvalidOperationException("Generic types cannot be used directly");
                case BaseVarType.Empty: 
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public static IState ConvertToTiType(this VarType origin, RefTo[] genericMap)
        {
            switch (origin.BaseType)
            {
                case BaseVarType.Generic: 
                    return genericMap[origin.GenericId.Value];
                case BaseVarType.ArrayOf:
                    return Tic.SolvingStates.Array.Of(ConvertToTiType(origin.ArrayTypeSpecification.VarType, genericMap));
                case BaseVarType.Fun:
                    return Fun.Of(
                        argTypes:   origin.FunTypeSpecification.Inputs.Select(type => ConvertToTiType(type, genericMap)).ToArray(),
                        returnType: ConvertToTiType(origin.FunTypeSpecification.Output, genericMap));
                default:
                    return origin.ConvertToTiType();
            }
        }
        //public static VarType GetVarType(this IState result, string varId, TiToLangTypeConverter converter)
        //    => converter.ToSimpleType( result.GetVarType(varId));
        //public static LangFunctionSignature GetFunctionOverload(this TiResult result, int nodeId,TiToLangTypeConverter converter)
        //{
        //    var overloadHmSignature = result.GetFunctionOverload(nodeId);
        //    if (overloadHmSignature == null)
        //        return null;
        //    return new LangFunctionSignature(
        //        converter.ToSimpleType(overloadHmSignature.ReturnType), 
        //        overloadHmSignature.ArgTypes.Select(o=>converter.ToSimpleType(o)).ToArray());
        //}

        /*public static VarType GetNodeTypeOrEmpty(this FinalizationResults result, int nodeId, TiToLangTypeConverter converter)
        {
            var hmType = result.GetNodeTypeOrNull(nodeId);
            if (hmType == null)
                return VarType.Empty;
            return converter.ToSimpleType(hmType);
        }*/
    }
}