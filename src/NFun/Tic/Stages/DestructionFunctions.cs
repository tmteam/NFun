using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages;

using System;

public class DestructionFunctions : IStateFunction {
    public static DestructionFunctions Singleton { get; } = new();

    public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode _, TicNode __)
        => true;

    public bool Apply(
        StatePrimitive ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.FitsInto(descendant)) {
            // Ancestor is a concrete type (from annotation or solved node).
            // It takes priority over Preferred — annotation beats default.
            // Exception: Optional inner elements preserve their Preferred type
            // to prevent ancestor widening (e.g., opt(I32) staying I32, not becoming Real).
            var resolved = descendant.Preferred != null
                           && descendantNode.IsOptionalElement
                           && descendant.CanBeConvertedTo(descendant.Preferred)
                ? (ITicNodeState)descendant.Preferred
                : ancestor;
            if (descendant.IsOptional && resolved != StatePrimitive.None)
                descendantNode.State = StateOptional.Of(resolved);
            else
                descendantNode.State = resolved;
        }

        return true;
    }

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode _, TicNode __)
        => true;

    public bool Apply(
        ConstraintsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        // None ≤ opt(T): no-op. None adds no information to an Optional constraint.
        // MaterializeOptionalFlags resolves the final type after all branches are processed.
        if (descendant == StatePrimitive.None && ancestor.IsOptional)
            return true;
        if (ancestor.CanBeConvertedTo(descendant))
            ancestorNode.State = ancestor.IsOptional
                ? StateOptional.Of(descendant)
                : (ITicNodeState)descendant;
        return true;
    }

    public bool Apply(
        ConstraintsState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // When ancestor is Optional and descendant is not, wrap as opt(descendant)
        // instead of flat-merging. This mirrors Apply(CS, StatePrimitive) which already
        // does StateOptional.Of(descendant) for non-None primitives.
        // Without this, a subsequent None descendant would collapse the flat-merged
        // ConstraintsState to None, losing the Optional semantic.
        if (ancestor.IsOptional && !descendant.IsOptional)
        {
            ancestorNode.State = new StateOptional(descendantNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }

        var result = ancestor.MergeOrNull(descendant);
        if (result == null)
            return false;

        if (result is StatePrimitive)
        {
            descendantNode.State = ancestorNode.State = result;
            return true;
        }

        if (ancestorNode.Type == TicNodeType.TypeVariable ||
            descendantNode.Type != TicNodeType.TypeVariable)
        {
            ancestorNode.State = result;
            descendantNode.State = new StateRefTo(ancestorNode);
        }
        else
        {
            descendantNode.State = result;
            ancestorNode.State = new StateRefTo(descendantNode);
        }

        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    public bool Apply(
        ConstraintsState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (descendant.FitsInto(ancestor))
        {
            if (ancestor.IsOptional) {
                // Wrap in Optional: ancestor becomes opt(descendant)
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + ancestorNode.Name.ToString().ToLower() + "'",
                    new StateRefTo(descendantNode));
                ancestorNode.State = new StateOptional(innerNode);
            } else {
                ancestorNode.State = new StateRefTo(descendantNode);
            }
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }

        TraceLog.WriteLine($"{descendant} does not fit into {ancestor}");

        // When the actual descendant contains Optional-wrapped elements that
        // the stale constraint snapshot doesn't know about (e.g. arr(opt(T))
        // vs snapshot arr(U8)), adopt the actual descendant directly.
        if (DescendantHasOptionalLift(ancestor.Descendant, descendant))
        {
            ancestorNode.State = new StateRefTo(descendantNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }

        // Reverse Optional lift: snapshot has Optional elements that actual descendant
        // doesn't (e.g., snapshot arr(opt(U8)) from IsOptional LCA, actual arr(U8)).
        // Use the snapshot (which includes Optional) and destruct element-by-element.
        if (ancestor.Descendant is StateArray ancArr && descendant is StateArray descArr
            && ancArr.Element is StateOptional && descArr.Element is not StateOptional)
        {
            TraceLog.WriteLine($"  Reverse Optional lift: snapshot={ancestor.Descendant}, actual={descendant}");
            ancestorNode.State = ancestor.Descendant;
            descendantNode.RemoveAncestor(ancestorNode);
            return Apply(ancArr, descArr, ancestorNode, descendantNode);
        }

        if (descendant.GetType() == ancestor.Descendant?.GetType())
        {
            // Use the snapshot type and recursively destruct element-by-element.
            // Preserve Optional wrapper when ancestor has IsOptional flag.
            // Pass innerNode (not ancestorNode) to recursive Apply so it doesn't overwrite the Optional.
            TicNode destructTarget;
            if (ancestor.IsOptional)
            {
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + ancestorNode.Name.ToString().ToLower() + "'",
                    ancestor.Descendant);
                ancestorNode.State = new StateOptional(innerNode);
                destructTarget = innerNode;
            }
            else
            {
                ancestorNode.State = ancestor.Descendant;
                destructTarget = ancestorNode;
            }
            descendantNode.RemoveAncestor(ancestorNode);
            return descendant switch {
                StateArray array =>
                    Apply((StateArray)ancestor.Descendant, array, destructTarget, descendantNode),
                StateFun fun =>
                    Apply((StateFun)ancestor.Descendant, fun, destructTarget, descendantNode),
                StateStruct @struct =>
                    Apply((StateStruct)ancestor.Descendant, @struct, destructTarget, descendantNode),
                StateOptional opt =>
                    Apply((StateOptional)ancestor.Descendant, opt, destructTarget, descendantNode),
                _ => throw new NotSupportedException($"type {descendant} is not supported for destruction")
            };
        }

        TraceLog.WriteLine($"{descendant} completely not fit into {ancestor}");
        return true;
    }

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor is StateOptional opt)
        {
            if (descendant.Name != PrimitiveTypeName.None)
            {
                // T ≤ opt(T): constrain the Optional's element with T
                SolvingFunctions.Destruction(descendantNode, opt.ElementNode);
                descendantNode.RemoveAncestor(ancestorNode);
            }

            // None ≤ opt(T) is always valid (no-op)
            return true;
        }

        return false;
    }

    public bool Apply(
        ICompositeState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.FitsInto(descendant))
        {
            if (descendant.IsOptional) {
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + descendantNode.Name.ToString().ToLower() + "'",
                    new StateRefTo(ancestorNode));
                descendantNode.State = new StateOptional(innerNode);
            } else {
                descendantNode.State = new StateRefTo(ancestorNode);
            }
            descendantNode.RemoveAncestor(ancestorNode);
        }
        else if (ancestor is StateStruct ancStruct
                 && descendant.HasDescendant && descendant.Descendant is StateStruct)
        {
            // Transform descendant constrains to struct, then destruct field-by-field
            var descStruct = SolvingFunctions.TransformToStructOrNull(descendant, ancStruct);
            if (descStruct != null)
            {
                descendantNode.State = descStruct;
                descendantNode.RemoveAncestor(ancestorNode);
                Apply(ancStruct, descStruct, ancestorNode, descendantNode);
            }
            else
            {
                TraceLog.WriteLine($"{ancestor} does not fit into {descendant}");
            }
        }
        else if (ancestor is StateOptional ancOpt)
        {
            if (descendant.HasDescendant && descendant.Descendant is StateOptional)
            {
                var descOpt = SolvingFunctions.TransformToOptionalOrNull(descendantNode.Name, descendant);
                if (descOpt != null)
                {
                    descendantNode.State = descOpt;
                    descendantNode.RemoveAncestor(ancestorNode);
                    Apply(ancOpt, descOpt, ancestorNode, descendantNode);
                }
                else
                {
                    TraceLog.WriteLine($"{ancestor} does not fit into {descendant}");
                }
            }
            else if (descendant.IsOptional)
            {
                // Descendant has IsOptional flag — materialize to Optional, then
                // do element-level destruction (opt vs opt).
                var innerCs = ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable);
                innerCs.Preferred = descendant.Preferred;
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + descendantNode.Name.ToString().ToLower() + "'", innerCs);
                descendantNode.State = new StateOptional(innerNode);
                descendantNode.RemoveAncestor(ancestorNode);
                Apply(ancOpt, (StateOptional)descendantNode.State, ancestorNode, descendantNode);
            }
            else
            {
                // Implicit lift: T ≤ opt(T). Destruct descendant with Optional's element.
                SolvingFunctions.Destruction(descendantNode, ancOpt.ElementNode);
                descendantNode.RemoveAncestor(ancestorNode);
            }
        }
        else
        {
            TraceLog.WriteLine($"{ancestor} does not fit into {descendant}");
        }

        return true;
    }

    public bool Apply(StateOptional ancestor, StateOptional descendant, TicNode ancestorNode, TicNode descendantNode) =>
        SolvingFunctions.Destruction(descendant.ElementNode, ancestor.ElementNode);

    public bool Apply(StateArray ancestor, StateArray descendant, TicNode ancestorNode, TicNode descendantNode) =>
        SolvingFunctions.Destruction(descendant.ElementNode, ancestor.ElementNode);

    public bool Apply(StateFun ancestor, StateFun descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.ArgsCount == descendant.ArgsCount)
            for (int i = 0; i < ancestor.ArgsCount; i++)
                SolvingFunctions.Destruction(descendant.ArgNodes[i], ancestor.ArgNodes[i]);
        SolvingFunctions.Destruction(ancestor.RetNode, descendant.RetNode);
        return true;
    }

    public bool Apply(StateStruct ancestor, StateStruct descendant, TicNode ancestorNode, TicNode descendantNode) {
        // Destruct field-by-field: for each ancestor field, find matching descendant field.
        var sameFieldCount = 0;
        foreach (var (key, ancFieldNode) in ancestor.Fields)
        {
            var descFieldNode = descendant.GetFieldOrNull(key);
            if (descFieldNode == null)
                continue; // descendant may have fewer fields (struct width subtyping)

            if (SolvingFunctions.Destruction(descFieldNode, ancFieldNode))
                sameFieldCount++;
        }

        // Only redirect if all fields match AND field nodes actually resolved to the
        // same node. This prevents overwriting an LCA result when field types differ
        // (e.g. I32 ≤ Re both destruct successfully but aren't equal).
        if (sameFieldCount == ancestor.FieldsCount && sameFieldCount == descendant.FieldsCount)
        {
            bool fieldsEquivalent = true;
            foreach (var (key, ancFieldNode) in ancestor.Fields)
            {
                var descFieldNode = descendant.GetFieldOrNull(key);
                if (ancFieldNode.GetNonReference() != descFieldNode.GetNonReference())
                {
                    fieldsEquivalent = false;
                    break;
                }
            }
            if (fieldsEquivalent)
                ancestorNode.State = new StateRefTo(descendantNode);
        }

        return true;
    }

    /// <summary>
    /// Checks if the actual descendant composite contains Optional-wrapped elements
    /// that the stale constraint descendant doesn't (e.g. arr(opt(T)) vs arr(U8)).
    /// This happens when Phase 2 wraps array/struct elements in Optional after
    /// the constraint's Descendant was already set.
    /// </summary>
    private static bool DescendantHasOptionalLift(ITicNodeState staleDescendant, ICompositeState actual) =>
        (staleDescendant, actual) switch {
            (StateArray staleArr, StateArray actualArr) =>
                actualArr.Element is StateOptional && staleArr.Element is not StateOptional,
            (StateStruct staleStr, StateStruct actualStr) =>
                HasAnyOptionalLiftedField(staleStr, actualStr),
            _ => false
        };

    private static bool HasAnyOptionalLiftedField(StateStruct stale, StateStruct actual) {
        foreach (var (key, actualFieldNode) in actual.Fields) {
            var staleFieldNode = stale.GetFieldOrNull(key);
            if (staleFieldNode == null) continue;
            if (actualFieldNode.State is StateOptional && staleFieldNode.State is not StateOptional)
                return true;
        }
        return false;
    }
}
