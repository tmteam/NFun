namespace NFun.Tic.Algebra;

using SolvingStates;
using static SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    /// <summary>
    /// Returns most possible abstract type, that can be represented by current state (without convertion)
    /// </summary>
    public static ITicNodeState Abstractest(this ITicNodeState a) =>
        a switch {
            StateRefTo aref => aref.Element.Abstractest(),
            ConstraintsState cs => cs.IsComparable ? cs : cs.HasAncestor ? cs.Ancestor : Any,
            StatePrimitive => a,
            StateArray arr => StateArray.Of(arr.Element.Abstractest()),
            StateOptional opt => AbstractestOptional(opt),
            StateFun f => AbstractestFun(f),
            StateStruct => a,
            _ => a
        };

    private static ITicNodeState AbstractestOptional(StateOptional opt) {
        var inner = opt.Element.Abstractest();
        // opt(any) = any
        return inner== Any ? Any : StateOptional.Of(inner);
    }

    private static ITicNodeState AbstractestFun(StateFun f) {
        var returnNode = TicNode.CreateInvisibleNode(f.ReturnType.Abstractest());
        var argNodes = new TicNode[f.ArgsCount];
        for (int i = 0; i < f.ArgsCount; i++)
            argNodes[i] = TicNode.CreateInvisibleNode(f.ArgNodes[i].State.Concretest());

        return StateFun.Of(argNodes, returnNode);
    }
}
