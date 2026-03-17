namespace NFun.Tic.Algebra;

using System.Collections.Generic;
using SolvingStates;

public static partial class StateExtensions {
    /// <summary>
    /// Returns most possible concrete type, that can be represented by current state (without convertion)
    /// </summary>
    public static ITicNodeState Concretest(this ITicNodeState a) =>
        a switch {
            StatePrimitive => a,
            ConstraintsState cs => cs.HasDescendant
                ? cs.Descendant.Concretest()
                : ConstraintsState.Of(isComparable: cs.IsComparable),
            StateRefTo aref => aref.Element.Concretest(),
            StateArray arr => StateArray.Of(arr.Element.Concretest()),
            StateOptional opt => ConcretestOptional(opt),
            StateFun f => ConcretestFun(f),
            StateStruct s => s.ConcretestStruct(),
            _ => a
        };

    private static ITicNodeState ConcretestOptional(StateOptional opt) {
        var inner = opt.Element.Concretest();
        // opt(any) = any
        return inner.Equals(StatePrimitive.Any) ? StatePrimitive.Any : StateOptional.Of(inner);
    }

    private static ITicNodeState ConcretestFun(StateFun f) {
        // return type is covariant, arg types are contravariant
        var returnNode = TicNode.CreateInvisibleNode(f.ReturnType.Concretest());
        var argNodes = new TicNode[f.ArgsCount];
        for (int i = 0; i < f.ArgsCount; i++)
            argNodes[i] = TicNode.CreateInvisibleNode(f.ArgNodes[i].State.Abstractest());

        return StateFun.Of(argNodes, returnNode);
    }

    private static StateStruct ConcretestStruct(this StateStruct s) {
        var nodes = new Dictionary<string, TicNode>(s.FieldsCount);
        bool changed = false;
        foreach (var (key, fieldNode) in s.Fields)
        {
            var nr = fieldNode.GetNonReference();
            if (nr != fieldNode)
                changed = true;
            nodes[key] = nr;
        }

        return changed ? new StateStruct(nodes, s.IsFrozen) : s;
    }
}
