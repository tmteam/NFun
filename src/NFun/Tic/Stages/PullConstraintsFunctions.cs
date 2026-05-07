using System.Runtime.CompilerServices;
using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

public class PullConstraintsFunctions : IStateFunction {
    public static PullConstraintsFunctions Singleton { get; } = new();

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
        // Propagate Preferred bidirectionally:
        // Upward (desc→anc): integer constants push I32 to array element types.
        // Downward (anc→desc): struct field chains push I32 to generic function results.
        if (ancestorCopy.Preferred == null && descendant.Preferred != null)
            ancestorCopy.Preferred = descendant.Preferred;
        if (descendant.Preferred == null && ancestor.Preferred != null)
            descendant.Preferred = ancestor.Preferred;
        // Propagate IsOptional flag (OR semantics): if descendant is optional,
        // the ancestor must be optional too. Uses AddDescendant(None) which sets the flag.
        if (descendant.IsOptional)
            ancestorCopy.AddDescendant(StatePrimitive.None);
        // F-bound StructBound is a third independent constraint dimension. Combining two upper
        // bounds is meet (Gcd = field union, narrower predicate). Both Pull and Push merge S via
        // Gcd — there is no Lca on S because upper bounds are never widened by ≤. Contractivity
        // preserved when both inputs are contractive (no wrapper structure changed).
        if (descendant.StructBound != null) {
            ancestorCopy.StructBound = ancestorCopy.StructBound == null
                ? SolvingFunctions.RewireStructBoundOwnership(descendant.StructBound, descendantNode, ancestorNode)
                : SolvingFunctions.GcdBound(ancestorCopy.StructBound, descendant.StructBound,
                                            ancestorNode, descendantNode);
            if (ancestorCopy.StructBound == null) return false; // conflict
        }
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
            var innerNode = TicNode.CreateTypeVariableNode("e" + ancestorNode.Name + "'", innerCs);
            // F-bound migrates to inner CS on Optional wrap. The bound lives on the recursive
            // variable (the inner element); self-RefTos in the bound that pointed at ancestorNode
            // (the outer) must be rewired to point at innerNode, otherwise opt(CS{S}) with
            // back-edges to outer-ancestor leaves dangling refs.
            if (ancestor.StructBound != null)
                innerCs.StructBound = SolvingFunctions.RewireStructBoundOwnership(
                    ancestor.StructBound, ancestorNode, innerNode);
            descOpt.ElementNode.AddAncestor(innerNode);
            ancestorNode.State = new StateOptional(innerNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        // Descendant is StateStruct and ancestor has F-bound. The descendant struct is the
        // candidate value; its fields contribute to the bound's required field set. GcdBound
        // merges desc as another bound contributor (treats StateStruct as "CS-with-S = struct itself").
        if (descendant is StateStruct descStruct && ancestor.StructBound != null)
        {
            var copy = ancestor.GetCopy();
            copy.AddDescendant(descStruct);
            var merged = SolvingFunctions.GcdBound(
                copy.StructBound, descStruct, ancestorNode, descendantNode);
            if (merged == null) return false;
            copy.StructBound = merged;
            var simplified = copy.SimplifyOrNull();
            if (simplified == null) return false;
            ancestorNode.State = simplified;
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
                // Descendant has F-bound (struct shape) but ancestor is StateArray —
                // structural conflict (struct ≠ array).
                if (descendant.StructBound != null) return false;
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
                // F-bound vs Fun is a structural conflict.
                if (descendant.StructBound != null) return false;
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
                // F-bound flowing INTO concrete struct. Descendant carries S = upper-bound shape;
                // merge S into the produced struct so width-required fields become explicit
                // constraints. GcdBound treats result + S as two parallel descendants, returning
                // their meet (field union).
                if (descendant.StructBound != null)
                {
                    var merged = SolvingFunctions.GcdBound(
                        result, descendant.StructBound, descendantNode, descendantNode);
                    if (merged == null) return false;
                    result = merged;
                }
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
                // If descendant's struct is opt-sourced, propagate the marker to the ancestor —
                // both share the same merged identity and the cycle-rescue rule must apply uniformly.
                if (result.IsOptionalSourced) ancStruct.IsOptionalSourced = true;
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
                        var innerCsCopy = descendant.GetCopy();
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name + "'", innerCsCopy);
                        // Rewire StructBound self-refs to new inner owner.
                        if (innerCsCopy.StructBound != null)
                            innerCsCopy.StructBound = SolvingFunctions.RewireStructBoundOwnership(
                                innerCsCopy.StructBound, descendantNode, innerNode);
                        innerNode.AddAncestor(ancOpt.ElementNode);
                        descendantNode.State = new StateOptional(innerNode);
                        descendantNode.RemoveAncestor(ancestorNode);
                        return true;
                    }
                    // When descendant is already marked Optional (from None in another branch),
                    // materialize as opt(inner) and connect inner to ancestor's element.
                    // This prevents IsOptional from leaking through the implicit lift redirect
                    // (e.g., SetCoalesce's opt(U) ancestor — U must NOT inherit IsOptional).
                    if (descendant.IsOptional)
                    {
                        var innerCs = ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable);
                        innerCs.Preferred = descendant.Preferred;
                        var innerNode = TicNode.CreateTypeVariableNode(
                            "e" + descendantNode.Name + "'", innerCs);
                        // F-bound migrates to inner CS on Optional materialization.
                        if (descendant.StructBound != null)
                            innerCs.StructBound = SolvingFunctions.RewireStructBoundOwnership(
                                descendant.StructBound, descendantNode, innerNode);
                        innerNode.IsOptionalElement = true;
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
                // Create inner CS for the Optional element. When the ancestor's descendant
                // is itself Optional (e.g., opt(I32) from a named type snapshot), unwrap it:
                // the outer Optional wrapper consumes the Optional layer.
                var innerCopy = ancCon.GetCopy();
                if (innerCopy.HasDescendant && innerCopy.Descendant is StateOptional innerOptDesc) {
                    var pref = innerCopy.Preferred;
                    innerCopy = ConstraintsState.Of(innerOptDesc.Element, innerCopy.Ancestor, innerCopy.IsComparable);
                    innerCopy.Preferred = pref;
                }
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + ancestor.ElementNode.Name + "'", innerCopy);
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
        // Merge identity bidirectionally across the Pull struct≤struct edge. After width
        // propagation makes field sets equal, both sides represent the same merged value — they
        // must agree on TypeName and IsOptionalSourced. The merge rule lives in
        // StateStruct.MergedTypeName. On TypeName conflict (both named, names differ) reject the
        // Pull — declared `type a` ≤ declared `type b` is a real type error.
        var mergedName = StateStruct.MergedTypeName(ancestor.TypeName, descendant.TypeName);
        if (mergedName == null && ancestor.TypeName != null && descendant.TypeName != null) {
            TraceLog.WriteLine($"    BLOCKED: TypeName conflict {ancestor.TypeName} vs {descendant.TypeName}");
            return false;
        }
        ancestor.TypeName = descendant.TypeName = mergedName;
        var mergedOptSourced = StateStruct.MergedIsOptionalSourced(ancestor.IsOptionalSourced, descendant.IsOptionalSourced);
        ancestor.IsOptionalSourced = descendant.IsOptionalSourced = mergedOptSourced;
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
