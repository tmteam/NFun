using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.SyntaxParsing.SyntaxNodes;
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

        public static string GetFunAlias(this UserFunctionDefinitionSyntaxNode syntaxNode)
            => GetFunAlias(syntaxNode.Id, syntaxNode.Args.Count);

        public static ITicNodeState GetTicFunType(this IFunctionSignature functionBase) =>
            StateFun.Of(
                functionBase.ArgTypes.SelectToArray(a => a.ConvertToTiType()),
                functionBase.ReturnType.ConvertToTiType());

        public static ITicNodeState GetTicFunType(this GenericFunctionBase functionBase, StateRefTo[] genericMap) =>
            StateFun.Of(
                functionBase.ArgTypes.SelectToArray(a => a.ConvertToTiType(genericMap)),
                functionBase.ReturnType.ConvertToTiType(genericMap));
        public static ITicNodeState ConvertToTiType(this VarType origin) =>
            origin.BaseType switch
            {
                BaseVarType.Bool => StatePrimitive.Bool,
                BaseVarType.Int16 => StatePrimitive.I16,
                BaseVarType.Int32 => StatePrimitive.I32,
                BaseVarType.Int64 => StatePrimitive.I64,
                BaseVarType.UInt8 => StatePrimitive.U8,
                BaseVarType.UInt16 => StatePrimitive.U16,
                BaseVarType.UInt32 => StatePrimitive.U32,
                BaseVarType.UInt64 => StatePrimitive.U64,
                BaseVarType.Real => StatePrimitive.Real,
                BaseVarType.Char => StatePrimitive.Char,
                BaseVarType.Any => StatePrimitive.Any,
                BaseVarType.ArrayOf => StateArray.Of(
                    ConvertToTiType(origin.ArrayTypeSpecification.VarType)),
                BaseVarType.Fun => StateFun.Of(
                    argTypes: origin.FunTypeSpecification.Inputs.SelectToArray(ConvertToTiType),
                    returnType: ConvertToTiType(origin.FunTypeSpecification.Output)),
                BaseVarType.Struct => StateStruct.Of(origin.StructTypeSpecification.Select(d =>
                    new KeyValuePair<string, ITicNodeState>(d.Key, ConvertToTiType(d.Value)))),
                _ => throw new ArgumentOutOfRangeException($"Var type '{origin}' is not supported for convertion to FunTicType")
            };

        public static ITicNodeState ConvertToTiType(this VarType origin, StateRefTo[] genericMap) =>
            origin.BaseType switch
            {
                BaseVarType.Generic => genericMap[origin.GenericId.Value],
                BaseVarType.ArrayOf => StateArray.Of(
                    ConvertToTiType(origin.ArrayTypeSpecification.VarType, genericMap)),
                BaseVarType.Fun => StateFun.Of(
                    argTypes: origin.FunTypeSpecification.Inputs.SelectToArray(
                        type => ConvertToTiType(type, genericMap)),
                    returnType: ConvertToTiType(origin.FunTypeSpecification.Output, genericMap)),
                BaseVarType.Struct => StateStruct.Of(
                    origin.StructTypeSpecification.Select(s=> new KeyValuePair<string, ITicNodeState>(
                        key:   s.Key,
                        value: ConvertToTiType(s.Value, genericMap)))),
                _ => origin.ConvertToTiType()
            };
    }
}