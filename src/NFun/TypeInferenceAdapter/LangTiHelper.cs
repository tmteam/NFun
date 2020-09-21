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
        public static string GetArgAlias(string funAlias, string argId)
            =>  funAlias + "::" + argId;
        public static string GetFunAlias(string funId, int argsCount)
            =>  funId + "(" + argsCount+")";

        public static string GetFunAlias(this UserFunctionDefenitionSyntaxNode syntaxNode)
            => GetFunAlias(syntaxNode.Id, syntaxNode.Args.Count);

        public static ITicNodeState GetTicFunType(this IFunctionSignature functionBase)
        {
            return StateFun.Of(
                functionBase.ArgTypes.SelectToArray(a => a.ConvertToTiType()),
                functionBase.ReturnType.ConvertToTiType());
        }
        public static ITicNodeState GetTicFunType(this GenericFunctionBase functionBase, StateRefTo[] genericMap) =>
            StateFun.Of(
                functionBase.ArgTypes.SelectToArray(a => a.ConvertToTiType(genericMap)),
                functionBase.ReturnType.ConvertToTiType(genericMap));
        public static ITicNodeState ConvertToTiType(this VarType origin)
        {
            switch (origin.BaseType)
            {
                case BaseVarType.Bool:   return StatePrimitive.Bool;
                case BaseVarType.Int16:  return StatePrimitive.I16;
                case BaseVarType.Int32:  return StatePrimitive.I32;
                case BaseVarType.Int64:  return StatePrimitive.I64;
                case BaseVarType.UInt8:  return StatePrimitive.U8;
                case BaseVarType.UInt16: return StatePrimitive.U16;
                case BaseVarType.UInt32: return StatePrimitive.U32;
                case BaseVarType.UInt64: return StatePrimitive.U64;
                case BaseVarType.Real:   return StatePrimitive.Real;
                case BaseVarType.Char:   return StatePrimitive.Char;
                case BaseVarType.Any:    return StatePrimitive.Any;
                case BaseVarType.ArrayOf: return Tic.SolvingStates.StateArray.Of(ConvertToTiType(origin.ArrayTypeSpecification.VarType));
                case BaseVarType.Fun: 
                    return StateFun.Of(
                        argTypes: origin.FunTypeSpecification.Inputs.SelectToArray(ConvertToTiType), 
                        returnType: ConvertToTiType(origin.FunTypeSpecification.Output));
                case BaseVarType.Generic: 
                    throw new InvalidOperationException("Generic types cannot be used directly");
                case BaseVarType.Empty: 
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public static ITicNodeState ConvertToTiType(this VarType origin, StateRefTo[] genericMap)
        {
            switch (origin.BaseType)
            {
                case BaseVarType.Generic: 
                    return genericMap[origin.GenericId.Value];
                case BaseVarType.ArrayOf:
                    return Tic.SolvingStates.StateArray.Of(ConvertToTiType(origin.ArrayTypeSpecification.VarType, genericMap));
                case BaseVarType.Fun:
                    return StateFun.Of(
                        argTypes:   origin.FunTypeSpecification.Inputs.SelectToArray(type => ConvertToTiType(type, genericMap)),
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