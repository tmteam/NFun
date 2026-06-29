using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tic;
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

    /// <summary>
    /// Returns function alias that distinguishes extension from regular functions.
    /// Extension functions use "." prefix: ".f(1)" vs "f(1)".
    /// </summary>
    public static string GetFunAliasWithExtension(this UserFunctionDefinitionSyntaxNode syntaxNode)
        => syntaxNode.IsExtension
            ? "." + GetFunAlias(syntaxNode.Id, syntaxNode.Args.Count)
            : GetFunAlias(syntaxNode.Id, syntaxNode.Args.Count);

    public static ITicNodeState GetTicFunType(this IFunctionSignature functionBase) =>
        StateFun.Of(
            functionBase.ArgTypes.SelectToArray(a => a.ConvertToTiType()),
            functionBase.ReturnType.ConvertToTiType());

    internal static ITicNodeState GetTicFunType(this IFunctionSignature functionBase,
        INamedTypeFieldRegistry registry) =>
        registry == null
            ? functionBase.GetTicFunType()
            : StateFun.Of(
                functionBase.ArgTypes.SelectToArray(a => a.ConvertToTiType(registry)),
                functionBase.ReturnType.ConvertToTiType(registry));

    public static ITicNodeState GetTicFunType(this GenericFunctionBase functionBase, StateRefTo[] genericMap) =>
        StateFun.Of(
            functionBase.ArgTypes.SelectToArray(a => a.ConvertToTiType(genericMap)),
            functionBase.ReturnType.ConvertToTiType(genericMap));

    public static ITicNodeState ConvertToTiType(this FunnyType origin) =>
        origin.BaseType switch {
            BaseFunnyType.Bool => StatePrimitive.Bool,
            BaseFunnyType.Int8 => StatePrimitive.I8,
            BaseFunnyType.Int16 => StatePrimitive.I16,
            BaseFunnyType.Int32 => StatePrimitive.I32,
            BaseFunnyType.Int64 => StatePrimitive.I64,
            BaseFunnyType.UInt8 => StatePrimitive.U8,
            BaseFunnyType.UInt16 => StatePrimitive.U16,
            BaseFunnyType.UInt32 => StatePrimitive.U32,
            BaseFunnyType.UInt64 => StatePrimitive.U64,
            BaseFunnyType.Real => StatePrimitive.Real,
            BaseFunnyType.Char => StatePrimitive.Char,
            BaseFunnyType.Ip => StatePrimitive.Ip,
            BaseFunnyType.Any => StatePrimitive.Any,
            BaseFunnyType.None => StatePrimitive.None,
            BaseFunnyType.Custom => new StatePrimitiveCustom(origin.CustomTypeDefinition.Name, origin),
            BaseFunnyType.Optional => StateOptional.Of(ConvertToTiType(origin.OptionalTypeSpecification.ElementType)),
            BaseFunnyType.ArrayOf => StateArray.Of(ConvertToTiType(origin.ArrayTypeSpecification.FunnyType)),
            BaseFunnyType.Fun => StateFun.Of(
                argTypes: origin.FunTypeSpecification.Inputs.SelectToArray(ConvertToTiType),
                returnType: ConvertToTiType(origin.FunTypeSpecification.Output)),
            BaseFunnyType.Struct => StateStruct.Of(
                origin.StructTypeSpecification.Select(
                    d => new KeyValuePair<string, ITicNodeState>(d.Key, ConvertToTiType(d.Value))),
                origin.StructTypeSpecification.IsFrozen),
            BaseFunnyType.NamedStruct => ResolveNamedStruct(origin.NamedStructTypeName, null, null),
            _ => throw new ArgumentOutOfRangeException(
                $"Var type '{origin}' is not supported for convertion to FunTicType")
        };

    /// <summary>
    /// Per-call state for recursive NamedStruct resolution. Created lazily on first nested
    /// resolution; tracks the in-progress type names + their root TicNode placeholders so a
    /// re-entered name becomes a StateRefTo to the placeholder (closing a μ-cycle).
    /// </summary>
    private sealed class ResolveContext {
        public readonly Dictionary<string, int> Depth = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, TicNode> RootNode = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Convert FunnyType to TIC state with named type registry for recursive type resolution.
    /// </summary>
    internal static ITicNodeState ConvertToTiType(this FunnyType origin,
        INamedTypeFieldRegistry registry) =>
        ConvertToTiTypeInner(origin, registry, ctx: null);

    private static ITicNodeState ConvertToTiTypeInner(FunnyType origin,
        INamedTypeFieldRegistry registry, ResolveContext ctx) =>
        origin.BaseType switch {
            BaseFunnyType.Bool => StatePrimitive.Bool,
            BaseFunnyType.Int8 => StatePrimitive.I8,
            BaseFunnyType.Int16 => StatePrimitive.I16,
            BaseFunnyType.Int32 => StatePrimitive.I32,
            BaseFunnyType.Int64 => StatePrimitive.I64,
            BaseFunnyType.UInt8 => StatePrimitive.U8,
            BaseFunnyType.UInt16 => StatePrimitive.U16,
            BaseFunnyType.UInt32 => StatePrimitive.U32,
            BaseFunnyType.UInt64 => StatePrimitive.U64,
            BaseFunnyType.Real => StatePrimitive.Real,
            BaseFunnyType.Char => StatePrimitive.Char,
            BaseFunnyType.Ip => StatePrimitive.Ip,
            BaseFunnyType.Any => StatePrimitive.Any,
            BaseFunnyType.None => StatePrimitive.None,
            BaseFunnyType.Custom => new StatePrimitiveCustom(origin.CustomTypeDefinition.Name, origin),
            BaseFunnyType.Optional => StateOptional.Of(
                ConvertToTiTypeInner(origin.OptionalTypeSpecification.ElementType, registry, ctx)),
            BaseFunnyType.ArrayOf => StateArray.Of(
                ConvertToTiTypeInner(origin.ArrayTypeSpecification.FunnyType, registry, ctx)),
            BaseFunnyType.Fun => StateFun.Of(
                argTypes: origin.FunTypeSpecification.Inputs.SelectToArray(t => ConvertToTiTypeInner(t, registry, ctx)),
                returnType: ConvertToTiTypeInner(origin.FunTypeSpecification.Output, registry, ctx)),
            BaseFunnyType.Struct => StateStruct.Of(
                origin.StructTypeSpecification.Select(
                    d => new KeyValuePair<string, ITicNodeState>(d.Key, ConvertToTiTypeInner(d.Value, registry, ctx))),
                origin.StructTypeSpecification.IsFrozen),
            BaseFunnyType.NamedStruct => ResolveNamedStruct(origin.NamedStructTypeName, registry, ctx),
            _ => throw new ArgumentOutOfRangeException(
                $"Var type '{origin}' is not supported for convertion to FunTicType")
        };

    private static ITicNodeState ResolveNamedStruct(string typeName,
        INamedTypeFieldRegistry registry, ResolveContext ctx) {
        if (registry == null || !registry.TryGetFields(typeName, out var fields))
            return ConstraintsState.Empty;

        ctx ??= new ResolveContext();

        ctx.Depth.TryGetValue(typeName, out var depth);
        if (depth >= 1) {
            // At depth ≥ 1, return RefTo to the root TicNode for a true graph cycle
            // (Pottier-Rémy '05 §10.6). Cycle-aware Equals/Merge keep traversal bounded.
            if (ctx.RootNode.TryGetValue(typeName, out var rootNode)) {
                return new StateRefTo(rootNode);
            }
            var placeholderFields = new (string, ITicNodeState)[fields.Length];
            for (int i = 0; i < fields.Length; i++)
                placeholderFields[i] = (fields[i].name, WrapSolved(fields[i].type, typeName));
            var named = StateStruct.Of(true, placeholderFields);
            named.TypeName = typeName;
            return named;
        }

        var rootPlaceholder = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        ctx.RootNode[typeName] = rootPlaceholder;
        ctx.Depth[typeName] = depth + 1;
        try {
            var ticFields = new (string, ITicNodeState)[fields.Length];
            for (int i = 0; i < fields.Length; i++)
                ticFields[i] = (fields[i].name, ConvertToTiTypeInner(fields[i].type, registry, ctx));
            var rootStruct = StateStruct.Of(false, ticFields);
            rootStruct.TypeName = typeName;
            rootPlaceholder.State = rootStruct;
            return rootStruct;
        }
        finally {
            ctx.Depth[typeName] = depth;
            ctx.RootNode.Remove(typeName);
        }
    }

    /// <summary>
    /// Creates solved state for a named struct field.
    /// Recursive self-refs → ConstraintsState.Empty (resolved to Any during TIC Finalize,
    /// then converted to NamedStructOf by TicTypesConverter when inside named struct).
    /// Optional/Array wrappers preserved.
    /// </summary>
    private static ITicNodeState WrapSolved(FunnyType type, string expandingTypeName) =>
        type.BaseType switch {
            BaseFunnyType.NamedStruct when string.Equals(type.NamedStructTypeName, expandingTypeName,
                    StringComparison.OrdinalIgnoreCase)
                => ConstraintsState.Empty,
            BaseFunnyType.Optional => StateOptional.Of(
                WrapSolved(type.OptionalTypeSpecification.ElementType, expandingTypeName)),
            BaseFunnyType.ArrayOf => StateArray.Of(
                WrapSolved(type.ArrayTypeSpecification.FunnyType, expandingTypeName)),
            _ => type.ConvertToTiType()
        };

    public static ITicNodeState ConvertToTiType(this FunnyType origin, StateRefTo[] genericMap) =>
        ConvertToTiType(origin, genericMap, registry: null);

    /// <summary>
    /// Convert FunnyType to TIC state for a generic function signature instantiation.
    /// Threads both <paramref name="genericMap"/> (T0/T1/... → fresh StateRefTo) and a
    /// <paramref name="registry"/> for NamedStruct expansion.
    /// </summary>
    public static ITicNodeState ConvertToTiType(this FunnyType origin,
        StateRefTo[] genericMap, INamedTypeFieldRegistry registry) =>
        origin.BaseType switch {
            BaseFunnyType.Generic => genericMap[origin.GenericId.Value],
            BaseFunnyType.Optional => StateOptional.Of(
                ConvertToTiType(origin.OptionalTypeSpecification.ElementType, genericMap, registry)),
            BaseFunnyType.ArrayOf => StateArray.Of(
                ConvertToTiType(origin.ArrayTypeSpecification.FunnyType, genericMap, registry)),
            BaseFunnyType.Fun => StateFun.Of(
                argTypes: origin.FunTypeSpecification.Inputs.SelectToArray(
                    type => ConvertToTiType(type, genericMap, registry)),
                returnType: ConvertToTiType(origin.FunTypeSpecification.Output, genericMap, registry)),
            // Generic function constraints: struct is open ("at least these fields").
            // Row polymorphism: the function only requires listed fields, not exact match.
            BaseFunnyType.Struct => StateStruct.Of(
                origin.StructTypeSpecification.Select(
                    s => new KeyValuePair<string, ITicNodeState>(
                        key: s.Key,
                        value: ConvertToTiType(s.Value, genericMap, registry))),
                origin.StructTypeSpecification.IsFrozen,
                isOpen: true),
            BaseFunnyType.NamedStruct => ResolveNamedStruct(origin.NamedStructTypeName, registry, null),
            _ => origin.ConvertToTiType()
        };
}
