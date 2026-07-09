namespace NFun.Tic.Algebra;

using System;
using System.Collections.Generic;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    public static ITicNodeState UnifyOrNull(this ITicNodeState a, ITicNodeState b) => a.UnifyOrNull(b, null);

    // Unify with the explicit coinductive context (see AlgebraCycleContext) — this is a
    // bridge of the mutually-recursive family: struct-field Unify reaches Merge, whose
    // AddDescendant re-enters Lca, and whose GcdBound re-enters Gcd.
    internal static ITicNodeState UnifyOrNull(this ITicNodeState a, ITicNodeState b, AlgebraCycleContext ctx) {
        if (a.Equals(b))
            return a;

        if (a is StateRefTo ar)
            return ar.GetNonReference().UnifyOrNull(b, ctx);
        if (b is StateRefTo br)
            return a.UnifyOrNull(br.GetNonReference(), ctx);

        // Any is top of ALL types: None ≤ Any, Opt(T) ≤ Any
        if (a== Any)
            return b;
        if (b== Any)
            return a;

        if (a is ConstraintsState ac)
        {
            if (b is ConstraintsState bc)
                return ac.UnifyOrNull(bc, ctx);
            else if (b.FitsInto(ac, ctx))
                return b;
            else
                return null;
        }

        if (b is ConstraintsState)
            return b.UnifyOrNull(a, ctx);
        // Optional: Unify(Opt(A), Opt(B)) = Opt(Unify(A,B)), Unify(Opt, None) = None
        if (a is StateOptional aOpt)
        {
            if (b is StateOptional bOpt)
            {
                var elem = aOpt.Element.UnifyOrNull(bOpt.Element, ctx);
                return elem == null ? null : StateOptional.Of(elem);
            }
            if (b == None)
                return b; // None ≤ Opt(T), intersection = None
            return null;
        }
        if (b is StateOptional)
            return b.UnifyOrNull(a, ctx);

        if (a.GetType() != b.GetType())
            return null;
        // Same-type but not equal: different primitives or different custom types → incompatible
        if (a is StatePrimitive)
            return null;
        if (b is StatePrimitive)
            return null;
        if (a is StateArray aArr)
            return aArr.UnifyOrNull(b as StateArray, ctx);
        if (a is StateFun aFun)
            return aFun.UnifyOrNull(b as StateFun, ctx);
        if (a is StateStruct aStr)
            return aStr.UnifyOrNull(b as StateStruct, ctx);
        throw new NotSupportedException($"Unitype({a}, {b})");
    }

    // Unify(CS, CS) ≝ Merge — there is exactly ONE constraint-intersection operator
    // (Algebra_Merge.md / Algebra_Unify.md; debt #13). All axes (cmp, opt, S, Preferred)
    // are transported by the Merge core (S = S₁ ∪ S₂ since debt #12 closed).
    private static ITicNodeState UnifyOrNull(this ConstraintsState a, ConstraintsState b, AlgebraCycleContext ctx) =>
        a.MergeOrNull(b, ctx);

    private static ITicNodeState UnifyOrNull(this StateArray a, StateArray b, AlgebraCycleContext ctx) {
        var uniElement = a.Element.UnifyOrNull(b.Element, ctx);
        return uniElement == null ? null : StateArray.Of(uniElement);
    }

    private static ITicNodeState UnifyOrNull(this StateFun a, StateFun b, AlgebraCycleContext ctx) {
        if (a.ArgsCount != b.ArgsCount)
            return null;
        var argNodes = new TicNode[a.ArgsCount];
        for (int i = 0; i < a.ArgsCount; i++)
        {
            var uniArg = a.ArgNodes[i].State.UnifyOrNull(b.ArgNodes[i].State, ctx);
            if (uniArg == null)
                return null;
            argNodes[i] = TicNode.CreateInvisibleNode(uniArg);
        }

        var retArg = a.ReturnType.UnifyOrNull(b.ReturnType, ctx);
        if (retArg == null)
            return null;
        return StateFun.Of(argNodes, TicNode.CreateInvisibleNode(retArg));
    }

    private static ITicNodeState UnifyOrNull(this StateStruct a, StateStruct b, AlgebraCycleContext ctx) {
        // MutStruct and Struct are different type constructors — cannot unify
        if (a is StateMutableStruct != b is StateMutableStruct)
            return null;

        if (a.FieldsCount != b.FieldsCount)
            return null;
        var fields = new Dictionary<string, TicNode>();
        foreach (var aField in a.Fields)
        {
            var bField = b.GetFieldOrNull(aField.Key);
            if (bField == null)
                return null;
            var uniField = aField.Value.State.UnifyOrNull(bField.State, ctx);
            if (uniField == null)
                return null;
            fields.Add(aField.Key, TicNode.CreateInvisibleNode(uniField));
        }

        if (a is StateMutableStruct)
            return new StateMutableStruct(fields, true);
        return new StateStruct(fields, true) {
            IsOptionalSourced = StateStruct.MergedIsOptionalSourced(a.IsOptionalSourced, b.IsOptionalSourced),
            TypeName = StateStruct.MergedTypeName(a.TypeName, b.TypeName),
        };
    }
}
