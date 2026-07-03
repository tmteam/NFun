using System.Collections.Generic;
using System.Linq;

namespace NFun.Tic.SolvingStates;

/// <summary>Abstract base for positional ordered collection states. Uniform shape:
/// <see cref="Constructor"/> + <see cref="Arguments"/>. Legacy <see cref="StateArray"/> /
/// <see cref="StateFun"/> / <see cref="StateStruct"/> do not migrate under this base.</summary>
public abstract class StateComposite : ICompositeState {

    public abstract ConstructorKind Constructor { get; }

    /// <summary>Ordered type arguments. Length is fixed per constructor.</summary>
    public abstract CompositeArg[] Arguments { get; }

    public abstract ICompositeState GetNonReferenced();

    public bool HasAnyReferenceMember {
        get {
            var args = Arguments;
            for (int i = 0; i < args.Length; i++)
                if (args[i].Node.State is StateRefTo) return true;
            return false;
        }
    }

    public int MemberCount => Arguments.Length;
    public TicNode GetMember(int index) => Arguments[index].Node;
    public IEnumerable<TicNode> Members => Arguments.Select(a => a.Node);

    public virtual IEnumerable<TicNode> AllLeafTypes {
        get {
            foreach (var arg in Arguments) {
                if (arg.Node.State is ICompositeState composite) {
                    if (arg.Node.VisitMark == Tic.TicVisitMarks.CompositeLeaf) continue;
                    var prev = arg.Node.VisitMark;
                    arg.Node.VisitMark = Tic.TicVisitMarks.CompositeLeaf;
                    foreach (var leaf in composite.AllLeafTypes) yield return leaf;
                    arg.Node.VisitMark = prev;
                } else {
                    yield return arg.Node;
                }
            }
        }
    }

    public abstract ITypeState GetLastCommonAncestorOrNull(ITypeState otherType);

    /// <summary>True if any argument is mutable. Cycle-guarded (coinductive — assume
    /// not-mutable on re-entry).</summary>
    public virtual bool IsMutable {
        get {
            var args = Arguments;
            for (int i = 0; i < args.Length; i++)
            {
                var node = args[i].Node;
                if (node.VisitMark == Tic.TicVisitMarks.CompositeIsMutableCycle) continue;
                var prev = node.VisitMark;
                node.VisitMark = Tic.TicVisitMarks.CompositeIsMutableCycle;
                try {
                    if (node.State.IsMutable) return true;
                } finally {
                    node.VisitMark = prev;
                }
            }
            return false;
        }
    }

    /// <summary>True if all arguments are solved. Coinductive cycle guard (Amadio–Cardelli '93)
    /// — assume solved on re-entry.</summary>
    public virtual bool IsSolved {
        get {
            var args = Arguments;
            for (int i = 0; i < args.Length; i++)
            {
                var node = args[i].Node;
                if (node.VisitMark == Tic.TicVisitMarks.CompositeIsSolvedCycle) continue;
                var prev = node.VisitMark;
                node.VisitMark = Tic.TicVisitMarks.CompositeIsSolvedCycle;
                try {
                    if (!node.State.IsSolved) return false;
                } finally {
                    node.VisitMark = prev;
                }
            }
            return true;
        }
    }

    public virtual string Description => StateDescription;

    public abstract string PrintState(int depth);
    public string StateDescription => PrintState(0);

    public virtual bool CanBePessimisticConvertedTo(StatePrimitive primitive) =>
        primitive.Name == PrimitiveTypeName.Any;
}

/// <summary>One type argument of a <see cref="StateComposite"/>: TIC <see cref="Node"/>
/// plus per-argument <see cref="Variance"/>.</summary>
public readonly record struct CompositeArg(TicNode Node, Variance Variance);
