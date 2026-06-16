using System.Collections.Generic;
using System.Linq;

namespace NFun.Tic.SolvingStates;

/// <summary>
/// Abstract base for positional ordered collection states introduced for
/// mutable collections. Single-arg collections (List/FixedArray/Array/Set/Queue/Stack)
/// all share the unified <see cref="StateCollection"/> subclass — their
/// <see cref="ConstructorKind"/> lives as data, not as C# class identity.
/// Two-arg collections (Map) will get their own subclass.
///
/// Provides a uniform shape: <see cref="Constructor"/> (which member of the
/// <see cref="ConstructorLattice"/>) plus <see cref="Arguments"/> (ordered
/// type parameters, each tagged with its own variance).
///
/// Cross-cutting TIC machinery (LCA, Merge, Pull, Push, Destruction) is
/// expected to operate on <see cref="StateComposite"/> uniformly in Stages 2+.
/// Stage 1 introduces the shape only — no algorithm calls into this base
/// yet; the class exists so Stage 2 has somewhere to land.
///
/// <para>The legacy <see cref="StateArray"/>, <see cref="StateFun"/>, and
/// <see cref="StateStruct"/> do <b>not</b> migrate under this base. They have
/// shape-incompatible internals (named-field dictionaries, arg/ret split,
/// covariant element) that don't benefit from uniform <see cref="CompositeArg"/>
/// representation. The refactor scope is positional ordered collections.</para>
/// </summary>
public abstract class StateComposite : ICompositeState {

    /// <summary>Which constructor in the lattice this state represents.</summary>
    public abstract ConstructorKind Constructor { get; }

    /// <summary>
    /// Ordered type arguments. Length is fixed per constructor:
    /// 1 for Enumerable/FixedArray/Array/List/Set, 2 for Map.
    /// </summary>
    public abstract CompositeArg[] Arguments { get; }

    // ─── ICompositeState ────────────────────────────────────────────

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

    /// <summary>
    /// Mark used by <see cref="AllLeafTypes"/> as a coinductive cycle guard.
    /// Mirrors the same pattern in <see cref="StateArray"/> / <see cref="StateOptional"/>.
    ///
    /// All cycle-guard mark constants live in <see cref="Tic.TicVisitMarks"/>.
    /// </summary>
    public virtual IEnumerable<TicNode> AllLeafTypes {
        get {
            foreach (var arg in Arguments) {
                if (arg.Node.State is ICompositeState composite) {
                    // Cycle guard: recursive named types embedding a list (forest = {kids:list<forest>})
                    // would otherwise recurse forever.
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

    // ─── ITypeState ─────────────────────────────────────────────────

    public abstract ITypeState GetLastCommonAncestorOrNull(ITypeState otherType);

    // ─── ITicNodeState ──────────────────────────────────────────────

    /// <summary>
    /// True if any argument is mutable. Cycle-guarded for recursive composite
    /// types (e.g. <c>list&lt;list&lt;list&lt;…&gt;&gt;&gt;</c>) — coinductive
    /// "not yet known mutable from this branch" returns false to avoid divergence.
    /// </summary>
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

    /// <summary>
    /// True if all arguments are solved. Cycle-guarded for recursive composite
    /// types — coinductive bisimulation (Amadio-Cardelli '93): on re-entry,
    /// assume the cycle branch is already solved (return true for that slot).
    /// </summary>
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

/// <summary>
/// One type argument of a <see cref="StateComposite"/>.
///
/// <see cref="Node"/> is the TIC node carrying the argument's resolved state
/// (primitive, composite, RefTo, or unresolved constraints). <see cref="Variance"/>
/// is per-argument metadata used by Pull/Push and LCA decomposition rules.
///
/// All new collections introduced in Stages 2-5 use <see cref="Variance.Invariant"/>.
/// </summary>
public readonly record struct CompositeArg(TicNode Node, Variance Variance);
