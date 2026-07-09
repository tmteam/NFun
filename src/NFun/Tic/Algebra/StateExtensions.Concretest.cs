namespace NFun.Tic.Algebra;

using System.Collections.Generic;
using SolvingStates;

public static partial class StateExtensions {
    /// <summary>
    /// ↓A — PURE lattice projection onto the lower bound (Algebra_Concretest.md).
    /// Recursive over the descendant (D may be any state); Rule B keeps the
    /// Optional lift of an unsolved bound in flag form. Preferred is TRANSPORTED
    /// where the result is a ConstraintsState (flag form), but never CHOSEN —
    /// resolution choices live in the Solve* family. The Destruction-snapshot
    /// variant that preserves Preferred across materialization is
    /// <see cref="ConcretestSnapshot"/>.
    /// </summary>
    public static ITicNodeState Concretest(this ITicNodeState a) =>
        a switch {
            StatePrimitive => a,
            ConstraintsState cs => ConcretestConstraints(cs),
            StateRefTo aref => aref.Element.Concretest(),
            // Pure covariant rule: ↓(A[]) = (↓A)[] — no Preferred-carrier special case
            // (that arm moved to ConcretestSnapshot, debt #19).
            StateArray arr => StateArray.Of(arr.Element.Concretest()),
            StateOptional opt => ConcretestOptional(opt),
            StateFun f => ConcretestFun(f),
            StateStruct s => s.ConcretestStruct(),
            _ => a
        };

    private static ITicNodeState ConcretestConstraints(ConstraintsState cs) {
        var inner = cs.HasDescendant
            ? cs.Descendant.Concretest()
            : ConstraintsState.Of(isComparable: cs.IsComparable);
        if (cs.IsOptional) {
            if (inner == StatePrimitive.Any)
                return StatePrimitive.Any;
            return LiftOptional(cs, inner);
        }
        return inner;
    }

    /// <summary>
    /// Rule B (canonical Optional form; debt #30): opt(τ) requires solved τ — the
    /// Optional lift of an UNSOLVED inner bound stays in flag form [D..A]?, because
    /// materialising opt(fresh-unsolved-copy) creates a dead island no edge can refine;
    /// a solved inner is wrapped in Opt(inner). Preferred is TRANSPORTED (kept on the
    /// CS result), never chosen. Shared by ↓ (<see cref="Concretest"/>) and
    /// ↓ₛ (<see cref="ConcretestSnapshot"/>) — the delegation law ↓ₛ ≡ ↓ on hint-free
    /// states holds structurally for this arm. Caller handles inner = Any (Opt(Any) = Any).
    /// </summary>
    private static ITicNodeState LiftOptional(ConstraintsState cs, ITicNodeState inner) {
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
        // Common case: no field needs path compression — return the original
        // struct without allocating. The dictionary is created lazily on the
        // FIRST changed field (copying the unchanged prefix).
        Dictionary<string, TicNode> nodes = null;
        int index = 0;
        foreach (var (key, fieldNode) in s.Fields) {
            var nr = fieldNode.GetNonReference();
            if (nodes == null) {
                if (nr != fieldNode) {
                    nodes = new Dictionary<string, TicNode>(s.FieldsCount);
                    int i = 0;
                    foreach (var (prefixKey, prefixNode) in s.Fields) {
                        if (i++ == index) break;
                        nodes[prefixKey] = prefixNode;
                    }
                    nodes[key] = nr;
                }
            } else {
                nodes[key] = nr;
            }
            index++;
        }
        if (nodes == null) return s;
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
