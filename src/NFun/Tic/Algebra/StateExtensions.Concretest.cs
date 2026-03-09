namespace NFun.Tic.Algebra;

using System.Collections.Generic;
using NFun.Tic.SolvingStates;

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
            StateArray arr => StateArray.Of(arr.Element.Concretest()),
            StateOptional opt => StateOptional.Of(opt.Element.Concretest()),
            StateRefTo aref => aref.Element.Concretest(),
            StateFun f => f.Concretest(),
            StateStruct s => s.ConcretestFields(),
            _ => a
        };

    private static ITicNodeState Concretest(this StateFun f) {
        // return type is covariant, arg types are contravariant
        var returnNode = TicNode.CreateInvisibleNode(Concretest(f.ReturnType));
        var argNodes = new TicNode[f.ArgsCount];
        for (int i = 0; i < f.ArgsCount; i++)
            argNodes[i] = TicNode.CreateInvisibleNode(f.ArgNodes[i].State.Abstractest());

        return StateFun.Of(argNodes, returnNode);
    }

    private static StateStruct ConcretestFields(this StateStruct s) {
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
