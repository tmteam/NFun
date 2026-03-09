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
        if (ancestor.FitsInto(descendant))
            descendantNode.State = ancestor;

        return true;
    }

    public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode _, TicNode __)
        => true;

    public bool Apply(
        ConstraintsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.CanBeConvertedTo(descendant))
            ancestorNode.State = descendant;
        return true;
    }

    public bool Apply(
        ConstraintsState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        // When ancestor has only None as descendant (from if-else/array None branch)
        // and descendant is NOT also None-only, wrap ancestor in Optional instead of unifying.
        // This prevents None from "infecting" unconstrained type variables.
        // Example: if(cond) x else none → result = opt(x), x stays generic
        if (ancestor.HasDescendant
            && ancestor.Descendant is StatePrimitive { Name: PrimitiveTypeName.None }
            && !ancestor.HasAncestor && !ancestor.IsComparable
            && !(descendant.HasDescendant
                 && descendant.Descendant is StatePrimitive { Name: PrimitiveTypeName.None }))
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
            ancestorNode.State = new StateRefTo(descendantNode);
            descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }

        TraceLog.WriteLine($"{descendant} does not fit into {ancestor}");
        if (descendant.GetType() == ancestor.Descendant?.GetType())
        {
            ancestorNode.State = ancestor.Descendant;
            descendantNode.RemoveAncestor(ancestorNode);
            return descendant switch {
                StateArray array =>
                    Apply((StateArray)ancestor.Descendant, array, ancestorNode, descendantNode),
                StateFun fun =>
                    Apply((StateFun)ancestor.Descendant, fun, ancestorNode, descendantNode),
                StateStruct @struct =>
                    Apply((StateStruct)ancestor.Descendant, @struct, ancestorNode, descendantNode),
                StateOptional opt =>
                    Apply((StateOptional)ancestor.Descendant, opt, ancestorNode, descendantNode),
                _ => throw new NotSupportedException($"type {descendant} is not supported for destruction")
            };
        }

        TraceLog.WriteLine($"{descendant} completely not fit into {ancestor}");
        return true;
    }

    public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode _, TicNode __) {
        // None ≤ opt(T) and T ≤ opt(T) via implicit lift
        if (ancestor is StateOptional)
            return true;
        return false;
    }

    public bool Apply(
        ICompositeState ancestor, ConstraintsState descendant, TicNode ancestorNode, TicNode descendantNode) {
        if (ancestor.FitsInto(descendant))
        {
            descendantNode.State = new StateRefTo(ancestorNode);
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
}
