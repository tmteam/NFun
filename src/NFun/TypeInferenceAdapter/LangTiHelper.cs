using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.TypeInferenceAdapter;

public static class LangTiHelper {
    public static string GetArgAlias(string funAlias, string argId)
        => funAlias + "::" + argId;

    public static string GetFunAlias(string funId, int argsCount)
        => funId + "(" + argsCount + ")";

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

    public static ITicNodeState ConvertToTiType(this FunnyType origin) =>
        origin.BaseType switch {
            BaseFunnyType.Bool   => StatePrimitive.Bool,
            BaseFunnyType.Int16  => StatePrimitive.I16,
            BaseFunnyType.Int32  => StatePrimitive.I32,
            BaseFunnyType.Int64  => StatePrimitive.I64,
            BaseFunnyType.UInt8  => StatePrimitive.U8,
            BaseFunnyType.UInt16 => StatePrimitive.U16,
            BaseFunnyType.UInt32 => StatePrimitive.U32,
            BaseFunnyType.UInt64 => StatePrimitive.U64,
            BaseFunnyType.Real   => StatePrimitive.Real,
            BaseFunnyType.Char   => StatePrimitive.Char,
            BaseFunnyType.Ip     => StatePrimitive.Ip,
            BaseFunnyType.Any    => StatePrimitive.Any,
            BaseFunnyType.ArrayOf => StateArray.Of(
                ConvertToTiType(origin.ArrayTypeSpecification.FunnyType)),
            BaseFunnyType.Fun => StateFun.Of(
                argTypes: origin.FunTypeSpecification.Inputs.SelectToArray(ConvertToTiType),
                returnType: ConvertToTiType(origin.FunTypeSpecification.Output)),
            BaseFunnyType.Struct => StateStruct.Of(
                origin.StructTypeSpecification.Select(
                    d =>
                        new KeyValuePair<string, ITicNodeState>(d.Key, ConvertToTiType(d.Value))),
                origin.StructTypeSpecification.IsFrozen),
            _ => throw new ArgumentOutOfRangeException(
                $"Var type '{origin}' is not supported for convertion to FunTicType")
        };

    public static ITicNodeState ConvertToTiType(this FunnyType origin, StateRefTo[] genericMap) =>
        origin.BaseType switch {
            BaseFunnyType.Generic => genericMap[origin.GenericId.Value],
            BaseFunnyType.ArrayOf => StateArray.Of(
                ConvertToTiType(origin.ArrayTypeSpecification.FunnyType, genericMap)),
            BaseFunnyType.Fun => StateFun.Of(
                argTypes: origin.FunTypeSpecification.Inputs.SelectToArray(
                    type => ConvertToTiType(type, genericMap)),
                returnType: ConvertToTiType(origin.FunTypeSpecification.Output, genericMap)),
            BaseFunnyType.Struct => StateStruct.Of(
                origin.StructTypeSpecification.Select(
                    s => new KeyValuePair<string, ITicNodeState>(
                        key: s.Key,
                        value: ConvertToTiType(s.Value, genericMap))),
                origin.StructTypeSpecification.IsFrozen),
            _ => origin.ConvertToTiType()
        };
}
