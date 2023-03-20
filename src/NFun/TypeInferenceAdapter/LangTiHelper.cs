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
            functionBase.ArgTypes.SelectToArray(a => a.ConvertToTicType()),
            functionBase.ReturnType.ConvertToTicType());

    public static ITicNodeState GetTicFunType(this GenericFunctionBase functionBase, StateRefTo[] genericMap) =>
        StateFun.Of(
            functionBase.ArgTypes.SelectToArray(a => a.ConvertToTicType(genericMap)),
            functionBase.ReturnType.ConvertToTicType(genericMap));

    public static ITicNodeState ConvertToTicType(this FunnyType origin, bool forceAllowDefaultValues = false) =>
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
                ConvertToTicType(origin.ArrayTypeSpecification.FunnyType, forceAllowDefaultValues)),
            BaseFunnyType.Fun => StateFun.Of(
                argTypes: origin.FunTypeSpecification.Inputs.SelectToArray(i=>ConvertToTicType(i,forceAllowDefaultValues)),
                returnType: ConvertToTicType(origin.FunTypeSpecification.Output, forceAllowDefaultValues)),
            BaseFunnyType.Struct => StateStruct.Of(
                origin.StructTypeSpecification.Select(
                    d =>
                        new KeyValuePair<string, ITicNodeState>(d.Key, ConvertToTicType(d.Value,forceAllowDefaultValues))),
                isFrozen: origin.StructTypeSpecification.IsFrozen,
                allowDefaultValues: forceAllowDefaultValues || origin.StructTypeSpecification.AllowDefaultValues
                ),
            _ => throw new ArgumentOutOfRangeException(
                $"Var type '{origin}' is not supported for convertion to FunTicType")
        };

    public static ITicNodeState ConvertToTicType(this FunnyType origin, StateRefTo[] genericMap) =>
        origin.BaseType switch {
            BaseFunnyType.Generic => genericMap[origin.GenericId.Value],
            BaseFunnyType.ArrayOf => StateArray.Of(
                ConvertToTicType(origin.ArrayTypeSpecification.FunnyType, genericMap)),
            BaseFunnyType.Fun => StateFun.Of(
                argTypes: origin.FunTypeSpecification.Inputs.SelectToArray(
                    type => ConvertToTicType(type, genericMap)),
                returnType: ConvertToTicType(origin.FunTypeSpecification.Output, genericMap)),
            BaseFunnyType.Struct => StateStruct.Of(
                origin.StructTypeSpecification.Select(
                    s => new KeyValuePair<string, ITicNodeState>(
                        key: s.Key,
                        value: ConvertToTicType(s.Value, genericMap))),
                origin.StructTypeSpecification.IsFrozen),
            _ => origin.ConvertToTicType()
        };
}
