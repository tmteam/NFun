using System.Runtime.CompilerServices;
using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

public class PullConstraintsFunctions : IStateFunction {
    public static IStateFunction Singleton { get; } = new PullConstraintsFunctions();

    public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode _, TicNode __) =>
        descendant.CanBePessimisticConvertedTo(ancestor);

    public bool Apply(StatePrimitive ancestor, ConstraintsState descendant, TicNode _, TicNode __)
        => descendant.CanBeConvertedOptimisticTo(ancestor);

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode _, TicNode __)
        => descendant.CanBePessimisticConvertedTo(ancestor);

    public bool Apply(ConstraintsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        // None is handled by AddDescendant setting IsOptional flag — no special wrapping needed.
        return ApplyAncestorConstrains(ancestorNode, ancestor, descendant);
    }

    public bool Apply(
        ConstraintsState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        var ancestorCopy = ancestor.GetCopy();
        ancestorCopy.AddDescendant(descendant.Descendant);
        // Propagate Preferred from descendant (e.g., integer constants with Preferred=I32).
        if (ancestorCopy.Preferred == null && descendant.Preferred != null)
            ancestorCopy.Preferred = descendant.Preferred;
        // Propagate IsOptional flag (OR semantics): if descendant is optional,
        // the ancestor must be optional too. Uses AddDescendant(None) which sets the flag.
        if (descendant.IsOptional)
            ancestorCopy.AddDescendant(StatePrimitive.None);
        var result = ancestorCopy.SimplifyOrNull();
        if (result == null)
            return false;
        ancestorNode.State = result;
        return true;
    }

    public bool Apply(
        ConstraintsState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant is StateOptional descOpt)
        {
            if (ancestorNode.IsOptionalElement)
            {
                descOpt.ElementNode.AddAncestor(ancestorNode);
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            // Verify ancestor constraints are compatible with Optional.
            // If ancestor has constraints that reject composite types (e.g., IsComparable
            // for arithmetic, or HasAncestor with a non-Any bound), reject.
            var probe = ancestor.GetCopy();
            probe.AddDescendant(descendant);
            if (probe.SimplifyOrNull() == null)
                return false;
            // Wrap ancestor in Optional, connecting element nodes for covariant propagation.
            // The inner element gets a copy of ancestor constraints WITHOUT IsOptional —
            // the Optional wrapper consumes the IsOptional flag.
            var innerCs = ConstraintsState.Of(ancestor.Descendant, ancestor.Ancestor, ancestor.IsComparable);
            innerCs.Preferred = ancestor.Preferred;
            var innerNode = TicNode.CreateTypeVariableNode(
                "e" + ancestorNode.Name.ToString().ToLower() + "'",
                innerCs);
            descOpt.ElementNode.AddAncestor(innerNode);
            ancestorNode.State = new StateOptional(innerNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        return ApplyAncestorConstrains(ancestorNode, ancestor, descendant);
    }

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor is StateOptional opt)
        {
            descendantNode.RemoveAncestor(ancestorNode);
            if (descendant.Name != PrimitiveTypeName.None)
            {
                descendantNode.AddAncestor(opt.ElementNode);
            }
            return true;
        }
        return false;
    }

    public bool Apply(
        ICompositeState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        switch (ancestor)
        {
            case StateArray ancArray:
            {
                var result = SolvingFunctions.TransformToArrayOrNull(descendantNode.Name, descendant);
                if (result == null)
                    return false;
                result.ElementNode.AddAncestor(ancArray.ElementNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            case StateFun ancFun:
            {
                var result = SolvingFunctions.TransformToFunOrNull(
                    descendantNode.Name, descendant, ancFun);
                if (result == null)
                    return false;
                result.RetNode.AddAncestor(ancFun.RetNode);
                for (int i = 0; i < result.ArgsCount; i++)
                    ancFun.ArgNodes[i].AddAncestor(result.ArgNodes[i]);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            case StateStruct ancStruct:
            {
                var result = SolvingFunctions.TransformToStructOrNull(descendant, ancStruct);
                if (result == null)
                    return false;
                descendantNode.State = result;
                // Width propagation: descendant struct may have more fields than
                // ancestor (e.g., generic T constrained to {a} via SetFieldAccess,
                // but input provides {a,b}). Add extra fields to non-frozen ancestor
                // to preserve type info through generic constraint graph.
                if (!ancStruct.IsFrozen)
                {
                    foreach (var descField in result.Fields)
                    {
                        if (ancStruct.GetFieldOrNull(descField.Key) == null)
                        {
                            TraceLog.WriteLine($"    Width propagation (CS→Struct): adding field '{descField.Key}' to ancestor {ancestorNode.Name}");
                            ancStruct.AddField(descField.Key, descField.Value);
                            ancestorNode.State = ancStruct;
                        }
                    }
                }
                return true;
            }
            case StateOptional ancOpt:
            {
                var result = SolvingFunctions.TransformToOptionalOrNull(descendantNode.Name, descendant);
                if (result == null)
                {
                    if (descendant.HasDescendant && descendant.Descendant is StateStruct)
                    {
                        // Struct descendant with explicit Optional ancestor
                        // (e.g., ?.field sets opt(struct) ancestor on lambda param that already
                        // has struct descendant from generic function binding).
                        // Transform to opt(inner) carrying the struct constraints.
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name.ToString().ToLower() + "'",
                            descendant.GetCopy());
                        innerNode.AddAncestor(ancOpt.ElementNode);
                        descendantNode.State = new StateOptional(innerNode);
                        descendantNode.RemoveAncestor(ancestorNode);
                        return true;
                    }
                    // Implicit lift: T ≤ opt(T).
                    // Only redirect to element for primitive/empty constraints.
                    if (!descendant.HasDescendant || descendant.Descendant is StatePrimitive)
                    {
                        descendantNode.RemoveAncestor(ancestorNode);
                        if (!descendant.HasDescendant
                            || descendant.Descendant != StatePrimitive.None)
                            descendantNode.AddAncestor(ancOpt.ElementNode);
                    }
                    return true;
                }
                result.ElementNode.AddAncestor(ancOpt.ElementNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                return true;
            }
            default: return true;
        }
    }

    public bool Apply(StateArray ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ElementNode != ancestor.ElementNode)
        {
            // When descendant element is Optional but ancestor element is not,
            // the ancestor element needs to be wrapped in Optional too.
            // This happens during re-pull after PullNoneNode Phase 2 wraps inner elements.
            if (descendant.ElementNode.State is StateOptional descOpt
                && ancestor.ElementNode.State is ConstraintsState ancCon)
            {
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + ancestor.ElementNode.Name.ToString().ToLower() + "'",
                    ancCon.GetCopy());
                descOpt.ElementNode.AddAncestor(innerNode);
                ancestor.ElementNode.State = new StateOptional(innerNode);
                ancestor.ElementNode.IsOptionalElement = true;
            }
            else
            {
                descendant.ElementNode.AddAncestor(ancestor.ElementNode);
            }
        }
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    public bool Apply(StateFun ancestor, StateFun descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ArgsCount != ancestor.ArgsCount)
            return false;
        descendant.RetNode.AddAncestor(ancestor.RetNode);
        for (int i = 0; i < descendant.ArgsCount; i++)
            ancestor.ArgNodes[i].AddAncestor(descendant.ArgNodes[i]);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    public bool Apply(StateStruct ancestor, StateStruct descendant, TicNode ancestorNode, TicNode descendantNode) {
        TraceLog.WriteLine($"  Pull Struct<-Struct: anc={ancestorNode.Name}:{ancestor.StateDescription} desc={descendantNode.Name}:{descendant.StateDescription}");
        foreach (var ancField in ancestor.Fields)
        {
            var descField = descendant.GetFieldOrNull(ancField.Key);
            if (descField == null)
            {
                if (descendant.IsFrozen)
                {
                    TraceLog.WriteLine($"    BLOCKED: desc is frozen, cannot add field '{ancField.Key}'");
                    return false;
                }

                TraceLog.WriteLine($"    Adding field '{ancField.Key}' to desc");
                descendant.AddField(ancField.Key, ancField.Value);
                descendantNode.State = descendant;
            }
            else if (descField != ancField.Value)
            {
                // None field: skip connection. None ≤ opt(T) is valid for any T,
                // but None cannot be directly connected to a bare struct/array/fun ancestor.
                // The Optional compatibility is handled at the outer level.
                if (descField.GetNonReference().State == StatePrimitive.None)
                {
                    TraceLog.WriteLine($"    Field '{ancField.Key}': None desc → skip (handled by outer Optional)");
                    continue;
                }
                TraceLog.WriteLine($"    Field '{ancField.Key}': desc ≤ anc ({descField.State} ≤ {ancField.Value.State})");
                descField.AddAncestor(ancField.Value);
            }
        }

        // Width propagation: when descendant has extra fields and ancestor is mutable
        // (inferred/generic, not frozen), add them to ancestor. This preserves type
        // information through generic constraints (e.g., sort(T[],rule(T)->R):T[]
        // where T must carry all input struct fields, not just those accessed in rule).
        // Algebraic basis: desc ≤ anc, anc is being inferred → anc should be as
        // narrow (specific) as possible → absorb all descendant fields.
        if (!ancestor.IsFrozen)
        {
            foreach (var descField in descendant.Fields)
            {
                if (ancestor.GetFieldOrNull(descField.Key) == null)
                {
                    TraceLog.WriteLine($"    Width propagation: adding field '{descField.Key}' to ancestor");
                    ancestor.AddField(descField.Key, descField.Value);
                    ancestorNode.State = ancestor;
                }
            }
        }

        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }


    public bool Apply(StateOptional ancestor, StateOptional descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.ElementNode != ancestor.ElementNode)
            descendant.ElementNode.AddAncestor(ancestor.ElementNode);
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ApplyAncestorConstrains(TicNode ancestorNode, ConstraintsState ancestor, ITypeState typeDesc) {
        var ancestorCopy = ancestor.GetCopy();
        ancestorCopy.AddDescendant(typeDesc);
        var result = ancestorCopy.SimplifyOrNull();
        if (result == null)
            return false;
        ancestorNode.State = result;
        return true;
    }
}
