namespace NFun.UnitTests;

using Tic;
using Tic.SolvingStates;

public static class SolvingStates {
    public static ConstrainsState EmptyConstrains => ConstrainsState.Empty;

    public static ITypeState Array(ITicNodeState state) => StateArray.Of(state);

    public static ITicNodeState Constrains(ITicNodeState desc = null, StatePrimitive anc = null,
        bool isComparable = false)
        => ConstrainsState.Of(desc, anc, isComparable);

    public static StateFun Fun(ITicNodeState returnType)
        => StateFun.Of(System.Array.Empty<ITicNodeState>(), returnType);

    public static StateFun Fun(ITicNodeState argType, ITicNodeState returnType)
        => StateFun.Of(new[] { argType }, returnType);

    public static StateFun Fun(ITicNodeState arg1Type, ITicNodeState arg2Type, ITicNodeState retType) =>
        StateFun.Of(new[] { arg1Type, arg2Type }, retType);

    public static StateFun Fun(ITicNodeState arg1Type, ITicNodeState arg2Type, ITicNodeState arg3Type, ITicNodeState retType) =>
        StateFun.Of(new[] { arg1Type, arg2Type, arg3Type }, retType);

    public static StateFun Fun (ITypeState[] argTypes, ITypeState retType) => StateFun.Of(argTypes, retType);

    public static StateStruct EmptyStruct(bool isFrozen = true) => StateStruct.Empty(isFrozen);

    public static StateStruct Struct(string fieldName, ITicNodeState fieldState) => Struct(true, (fieldName, fieldState));

    public static StateStruct Struct(params (string, ITicNodeState)[] fields) => Struct(true, fields);

    public static StateStruct Struct(bool isFrozen, params (string, ITicNodeState)[] fields) =>
         StateStruct.Of(isFrozen, fields);

    public static StateRefTo Ref(TicNode node) => new(node);

    public static StateRefTo Ref(ITicNodeState state) => Ref(TicNode.CreateInvisibleNode(state));
}
