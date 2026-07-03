namespace NFun.Tic.Algebra;

using System.Collections.Generic;
using SolvingStates;

public static partial class StateExtensions {
    /// <summary>
    /// Returns most concrete type representable by current state.
    /// Preferred is metadata — Concretest preserves it when the result
    /// is a ConstraintsState (keeps Preferred for Destruction snapshots).
    /// For concrete types (Primitive, Composite), Preferred is not applicable.
    /// </summary>
    public static ITicNodeState Concretest(this ITicNodeState a) =>
        a switch {
            StatePrimitive => a,
            ConstraintsState cs => ConcretestConstraints(cs),
            StateRefTo aref => aref.Element.Concretest(),
            StateArray arr => StateArray.Of(ConcretestArrayElement(arr.Element)),
            StateOptional opt => ConcretestOptional(opt),
            StateFun f => ConcretestFun(f),
            StateStruct s => s.ConcretestStruct(),
            _ => a
        };

    /// <summary>
    /// Concretest for array element: if element is CS with Preferred that differs from Desc,
    /// preserve the CS (with Preferred) instead of collapsing to bare Primitive.
    /// This ensures array Desc snapshots carry Preferred through Destruction.
    /// </summary>
    private static ITicNodeState ConcretestArrayElement(ITicNodeState element) {
        if (element is ConstraintsState cs
            && cs.Preferred != null
            && cs.HasDescendant
            && cs.Descendant is StatePrimitive desc
            && !desc.Equals(cs.Preferred)
            && desc.CanBePessimisticConvertedTo(cs.Preferred)) {
            var result = ConstraintsState.Of(desc, isComparable: cs.IsComparable, isOptional: cs.IsOptional);
            result.Preferred = cs.Preferred;
            return result;
        }
        return element.Concretest();
    }

    private static ITicNodeState ConcretestConstraints(ConstraintsState cs) {
        var inner = cs.HasDescendant
            ? cs.Descendant.Concretest()
            : ConstraintsState.Of(isComparable: cs.IsComparable);
        if (cs.IsOptional) {
            // For Optional: use Preferred when narrower Desc available.
            if (cs.Preferred != null && inner is StatePrimitive ip
                && ip.CanBePessimisticConvertedTo(cs.Preferred))
                inner = cs.Preferred;
            if (inner == StatePrimitive.Any)
                return StatePrimitive.Any;
            // Rule B (canonical Optional form): opt(τ) requires solved τ. The Optional
            // lift of an unsolved bound stays in flag form [D..A]? — materialising
            // opt(fresh-unsolved-copy) here creates a dead island no edge can refine.
            // Ported from lang-mutable-collections (specs_tic CanonicalForms on the
            // branch; closes the dead-invisible-snapshot family incl. Bug#6).
            if (inner is ConstraintsState innerCs) {
                var lifted = ConstraintsState.Of(
                    innerCs.HasDescendant ? innerCs.Descendant : null,
                    innerCs.HasAncestor ? innerCs.Ancestor : null,
                    innerCs.IsComparable || cs.IsComparable,
                    isOptional: true);
                lifted.Preferred = cs.Preferred ?? innerCs.Preferred;
                return lifted;
            }
            if (inner is ITypeState { IsSolved: false } unsolvedType) {
                var lifted = ConstraintsState.Of(
                    unsolvedType, null, cs.IsComparable, isOptional: true);
                lifted.Preferred = cs.Preferred;
                return lifted;
            }
            return StateOptional.Of(inner);
        }
        return inner;
    }

    private static ITicNodeState ConcretestOptional(StateOptional opt) {
        var inner = opt.Element.Concretest();
        return inner == StatePrimitive.Any ? StatePrimitive.Any : StateOptional.Of(inner);
    }

    private static ITicNodeState ConcretestFun(StateFun f) {
        var returnNode = TicNode.CreateInvisibleNode(f.ReturnType.Concretest());
        var argNodes = new TicNode[f.ArgsCount];
        for (int i = 0; i < f.ArgsCount; i++)
            argNodes[i] = TicNode.CreateInvisibleNode(f.ArgNodes[i].State.Abstractest());
        return StateFun.Of(argNodes, returnNode);
    }

    private static StateStruct ConcretestStruct(this StateStruct s) {
        var nodes = new Dictionary<string, TicNode>(s.FieldsCount);
        bool changed = false;
        foreach (var (key, fieldNode) in s.Fields) {
            var nr = fieldNode.GetNonReference();
            if (nr != fieldNode) changed = true;
            nodes[key] = nr;
        }
        if (!changed) return s;
        if (s is StateMutableStruct)
            return new StateMutableStruct(nodes, s.IsFrozen, s.IsOpen);
        // Preserve TypeName/IsOptionalSourced through the path-compression copy so the named
        // identity follows the struct.
        return new StateStruct(nodes, s.IsFrozen, s.IsOpen) {
            IsOptionalSourced = s.IsOptionalSourced,
            TypeName = s.TypeName,
        };
    }
}
