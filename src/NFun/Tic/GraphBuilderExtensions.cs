using System;
using System.Collections.Generic;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;

namespace NFun.Tic;

/// <summary>Convenience overloads that compose <see cref="GraphBuilder"/> operations into NFun language constructs.</summary>
public static class GraphBuilderExtensions {

    public static void SetVar(this GraphBuilder b, string name, int node) {
        var namedNode = b.GetNamedNode(name);
        var idNode = b.GetOrCreateNode(node);
        if (idNode.State is ConstraintsState)
        {
            // Concrete StateFun → bind via RefTo so SetCall sees the function shape directly.
            if (namedNode.State is StateFun)
                idNode.State = new StateRefTo(namedNode);
            else
                namedNode.AddAncestor(idNode);
        }
        else
            throw new InvalidOperationException(
                $"Node {node} cannot be referenced by '{name}' because it is not constrained node.");
    }

    public static void SetIfElse(this GraphBuilder b, int[] conditions, int[] expressions, int resultId) {
        var result = b.GetOrCreateNode(resultId);
        foreach (var exprId in expressions)
        {
            var expr = b.GetOrCreateNode(exprId);
            expr.AddAncestor(result);
        }

        foreach (var condId in conditions)
            b.SetOrCreatePrimitive(condId, StatePrimitive.Bool);
    }

    public static void SetConst(this GraphBuilder b, int id, StatePrimitive type)
        => b.SetOrCreatePrimitive(id, type);

    public static void SetIntConst(this GraphBuilder b, int id, StatePrimitive desc)
        => b.SetGenericConst(id,
            desc: desc,
            anc: StatePrimitive.Real,
            preferred: StatePrimitive.Real);

    public static void SetGenericConst(this GraphBuilder b, int id,
        StatePrimitive desc = null, StatePrimitive anc = null, StatePrimitive preferred = null) {
        var node = b.GetOrCreateNode(id);
        if (node.State is ConstraintsState constrains)
        {
            constrains.AddAncestor(anc);
            constrains.AddDescendant(desc);
            constrains.Preferred = preferred;
        }
        else
            throw new InvalidOperationException();
    }

    public static void SetArrayConst(this GraphBuilder b, int id, StatePrimitive elementType) {
        var eNode = b.CreateVarType(elementType);
        var node = b.GetOrCreateNode(id);
        if (node.State is ConstraintsState c)
        {
            var arrayOf = StateArray.Of(eNode);
            if (c.CanBeConvertedTo(arrayOf))
            {
                node.State = arrayOf;
                return;
            }
        }
        else if (node.State is StateArray a)
        {
            if (a.Element.Equals(elementType))
                return;
        }

        throw new InvalidOperationException();
    }

    public static void SetStructConst(this GraphBuilder b, int id, StateStruct @struct) {
        if (!@struct.IsSolved)
            throw new InvalidOperationException();
        b.GetOrCreateStructNode(id, @struct);
    }

    /// <summary>Bind a TIC node to an Optional-typed constant (e.g. parameter default <c>x: int? = 5</c>).</summary>
    public static void SetOptionalConst(this GraphBuilder b, int id, StateOptional opt) {
        var node = b.GetOrCreateNode(id);
        if (node.State is ConstraintsState)
            node.State = opt;
        else if (!node.State.Equals(opt))
            throw new InvalidOperationException(
                $"Cannot set optional const at node {node.Name}: state already {node.State}");
    }

    public static void CreateLambda(this GraphBuilder b, int returnId, int lambdaId, params string[] varNames) {
        var args = b.GetNamedNodes(varNames);
        var ret = b.GetOrCreateNode(returnId);
        b.SetOrCreateLambda(lambdaId, args, ret);
    }

    public static void CreateLambda(this GraphBuilder b, int returnId, int lambdaId, ITypeState returnType,
        params string[] varNames) {
        var args = b.GetNamedNodes(varNames);
        var exprId = b.GetOrCreateNode(returnId);
        var returnTypeNode = b.CreateVarType(returnType);
        exprId.AddAncestor(returnTypeNode);
        b.SetOrCreateLambda(lambdaId, args, returnTypeNode);
    }

    public static StateRefTo SetStrictArrayInit(this GraphBuilder b, int resultIds, params int[] elementIds) {
        var elementType = b.CreateVarType();
        b.GetOrCreateArrayNode(resultIds, elementType);

        foreach (var id in elementIds)
        {
            elementType.BecomeReferenceFor(b.GetOrCreateNode(id));
            elementType.IsMemberOfAnything = true;
        }

        return new StateRefTo(elementType);
    }

    public static void SetSoftArrayInit(this GraphBuilder b, int resultIds, params int[] elementIds)
        => SetSoftArrayInit(b, resultIds, elementIds, elementAncestorHint: null);

    /// <summary>
    /// Variant with a pre-resolved element-type hint. When the caller already knows all elements
    /// share a named-struct type, seeding the element-LCA node with the resolved shape prevents
    /// the "None desc → skip" rule from leaking <c>none</c> through unresolved fields.
    /// </summary>
    public static void SetSoftArrayInit(this GraphBuilder b, int resultIds, int[] elementIds,
        ITicNodeState elementAncestorHint) {
        TicNode elementType;
        if (elementAncestorHint is ITypeState hintTypeState)
        {
            elementType = b.CreateVarType(hintTypeState);
        }
        else
        {
            elementType = b.CreateVarType();
        }
        b.GetOrCreateArrayNode(resultIds, elementType);
        foreach (var id in elementIds)
        {
            b.GetOrCreateNode(id).AddAncestor(elementType);
            elementType.IsMemberOfAnything = true;
        }
    }

    /// <summary>Lang-mode list-literal init — sibling of <see cref="SetSoftArrayInit(GraphBuilder, int, int[], ITicNodeState)"/>.</summary>
    public static void SetSoftListInit(this GraphBuilder b, int resultIds, params int[] elementIds) =>
        SetSoftListInit(b, resultIds, elementIds, elementAncestorHint: null);

    public static void SetSoftListInit(this GraphBuilder b, int resultIds, int[] elementIds,
        ITicNodeState elementAncestorHint) {
        TicNode elementType;
        if (elementAncestorHint is ITypeState hintTypeState)
            elementType = b.CreateVarType(hintTypeState);
        else
            elementType = b.CreateVarType();
        b.GetOrCreateListNode(resultIds, elementType);
        foreach (var id in elementIds)
        {
            b.GetOrCreateNode(id).AddAncestor(elementType);
            elementType.IsMemberOfAnything = true;
        }
    }

    public static void SetFieldAccess(this GraphBuilder b, int structNodeId, int opId, string fieldName,
        string sourceTypeNameHint = null) {
        // Open (row-poly) struct probe: "source has at least this field". Mutability must match
        // source's — StateStruct vs StateMutableStruct are incomparable in GetMergedStateOrNull.
        var existingNonRef = b.GetOrCreateNode(structNodeId).GetNonReference();
        StateStruct openProbe = existingNonRef.State is StateMutableStruct
            ? new StateMutableStruct(isOpen: true)
            : new StateStruct(isOpen: true);
        var node = b.GetOrCreateStructNode(structNodeId, openProbe)
            .GetNonReference();

        var state = (StateStruct)node.State;
        // Propagate source's named TypeName onto the open probe so LcaTypeName(t,t)=t survives
        // — without it, LCA with the anonymous probe drops both inputs to anonymous.
        if (state.TypeName == null && sourceTypeNameHint != null)
            state.TypeName = sourceTypeNameHint;
        var memberNode = state.GetFieldOrNull(fieldName);
        if (memberNode == null)
        {
            memberNode = b.CreateVarType();
            state.AddField(fieldName, memberNode);
            node.State = state;
        }

        // Named-type ancestor declares this field as Optional: reuse its field node so the
        // declared type survives narrowing.
        if (b.NamedTypeRegistry != null) {
            for (int ai = 0; ai < node.Ancestors.Count; ai++) {
                var ancNr = node.Ancestors[ai].GetNonReference();
                if (ancNr.State is StateStruct ancStruct) {
                    var ancField = ancStruct.GetFieldOrNull(fieldName);
                    if (ancField != null && ancField != memberNode
                        && ancField.GetNonReference().State is StateOptional) {
                        memberNode = ancField;
                        break;
                    }
                }
            }
        }

        b.MergeOrSetNode(opId, new StateRefTo(memberNode));
    }

    /// <summary>
    /// <c>x?.field</c> with <c>x: opt(struct{field: T}) → opt(T)</c>.
    /// opt(opt(T)) is flattened during Destruction (see FlattenNestedOptional).
    /// </summary>
    public static void SetSafeFieldAccess(this GraphBuilder b, int sourceNodeId, int opId, string fieldName) {
        // `?.` produces an IsOptionalSourced struct that can close a μ-cycle via Push.
        b.IsRecursion = true;
        var fieldTypeNode = b.CreateVarType();

        // IsOptionalSourced: Push restores the Optional break when this struct closes a
        // self-cycle (otherwise the loop is non-contractive struct→struct).
        var fields = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase) { { fieldName, fieldTypeNode } };
        var structState = new StateStruct(fields, isFrozen: false, isOpen: true) { IsOptionalSourced = true };
        var structNode = b.CreateVarType(structState);

        var sourceType = StateOptional.Of(structNode);

        var resultType = StateOptional.Of(fieldTypeNode);

        b.SetCall(
            new ITicNodeState[] { sourceType, resultType },
            new[] { sourceNodeId, opId });
    }

    /// <summary>
    /// <c>x?[i]</c> with <c>x: opt(arr(T)) → opt(T)</c>. Built as a single connected
    /// subgraph (mirrors <see cref="SetSafeFieldAccess"/> / <see cref="SetSafeMethodCall"/>):
    /// result.State set to Optional upfront so downstream edges see Optional from the start —
    /// the LCA-with-None pattern would drop the Optional layer through TransformToArrayOrNull.
    /// </summary>
    public static void SetSafeArrayAccess(this GraphBuilder b, int sourceNodeId, int indexNodeId, int resultNodeId) {
        var elemNode = b.CreateVarType();

        var arrNode = b.CreateVarType(StateArray.Of(elemNode));
        var sourceType = StateOptional.Of(arrNode);
        b.SetCallArgument(sourceType, sourceNodeId);

        b.SetCallArgument(StatePrimitive.I32, indexNodeId);

        // Result = opt(elemNode): elemNode is shared with the source's array shape so Pull
        // refines both together. None flows in via the standard None ≤ Opt(T) on the source edge.
        var resultNode = b.GetOrCreateNode(resultNodeId);
        var resultType = StateOptional.Of(elemNode);
        resultNode.State = SolvingFunctions.GetMergedStateOrNull(resultNode.State, resultType)
                           ?? throw TicErrors.CannotSetState(resultNode, resultType);
    }

    /// <summary>
    /// <c>x?.field(args)</c> with <c>x: opt(struct{field: (P1..Pn) → R}) → opt(R)</c>.
    /// Built as a single connected subgraph so one Pull cascade carries the field-function's
    /// arg/return types through to the call site (a three-stage build would re-pull lost edges).
    /// </summary>
    public static void SetSafeMethodCall(
        this GraphBuilder b, int sourceNodeId, int[] callArgIds, int resultNodeId, string fieldName) {
        // Same cycle-rescue marker as SetSafeFieldAccess — fn-field closures can produce μ-cycles.
        b.IsRecursion = true;

        // returnNode is shared between funNode and the result Optional so Pull threads the
        // source's concrete return type straight through.
        var paramNodes = new TicNode[callArgIds.Length];
        for (int i = 0; i < callArgIds.Length; i++)
            paramNodes[i] = b.CreateVarType();
        var returnNode = b.CreateVarType();

        var funNode = b.CreateVarType(StateFun.Of(paramNodes, returnNode));

        var fields = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase) { { fieldName, funNode } };
        var structState = new StateStruct(fields, isFrozen: false, isOpen: true) { IsOptionalSourced = true };
        var structNode = b.CreateVarType(structState);

        // source ≤ opt(struct{fieldName: (P1..Pn) → R}) — single subtype edge.
        b.SetCallArgument(StateOptional.Of(structNode), sourceNodeId);

        // call argᵢ ≤ Pᵢ.
        for (int i = 0; i < callArgIds.Length; i++)
            b.SetCallArgument(new StateRefTo(paramNodes[i]), callArgIds[i]);

        // Result = opt(returnNode): returnNode shares with funNode's return slot so Pull
        // resolves both together. None from the source flows via the standard None ≤ opt(T).
        var resultNode = b.GetOrCreateNode(resultNodeId);
        var resultType = StateOptional.Of(returnNode);
        resultNode.State = SolvingFunctions.GetMergedStateOrNull(resultNode.State, resultType)
                           ?? throw TicErrors.CannotSetState(resultNode, resultType);
    }

    /// <summary><c>left ?? right</c>: <c>(opt(U), V) → LCA(U, V)</c>. Optional right is allowed.</summary>
    public static void SetCoalesce(this GraphBuilder b, int leftId, int rightId, int resultId) {
        // U is the unwrap target — must reject both the implicit Optional lift (via
        // IsSignatureParam) and CS×CS IsOptional OR-fusion (via IsForcedNonOptional, the
        // negative-skolem flag in IntersectIntervalsOrNull).
        var elemNode = b.CreateVarType();
        elemNode.IsSignatureParam = true;
        elemNode.IsForcedNonOptional = true;

        var leftType = StateOptional.Of(elemNode);
        b.SetCallArgument(leftType, leftId);

        var resultNode = b.GetOrCreateNode(resultId);
        elemNode.AddAncestor(resultNode);
        var rightNode = b.GetOrCreateNode(rightId);
        rightNode.AddAncestor(resultNode);
    }

    /// <summary>
    /// <c>!</c> (force unwrap) — TIC special form <c>(opt(T)) → T</c>. Same rigidity flags as
    /// <see cref="SetCoalesce"/> so nested-Optional inputs resolve at the inner shape.
    /// </summary>
    public static void SetForceUnwrap(this GraphBuilder b, int argId, int resultId) {
        var elemNode = b.CreateVarType();
        elemNode.IsSignatureParam = true;
        elemNode.IsForcedNonOptional = true;
        var argType = StateOptional.Of(elemNode);
        b.SetCallArgument(argType, argId);
        var resultNode = b.GetOrCreateNode(resultId);
        elemNode.AddAncestor(resultNode);
    }

    public static void SetMutableStructInit(this GraphBuilder b, string[] fieldNames, int[] fieldExpressionIds, int id) {
        var fields = new Dictionary<string, TicNode>(fieldNames.Length, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < fieldNames.Length; i++)
        {
            if (fields.ContainsKey(fieldNames[i]))
                throw TicErrors.CannotSetState(
                    b.GetOrCreateNode(id),
                    new StateMutableStruct(fields, false));
            fields.Add(fieldNames[i], b.GetOrCreateNode(fieldExpressionIds[i]));
        }

        var mutStruct = new StateMutableStruct(fields, false);
        var alreadyExists = b.GetOrCreateNode(id);
        if (alreadyExists.State is ConstraintsState)
        {
            alreadyExists.State = mutStruct;
        }
        else
        {
            alreadyExists.State = SolvingFunctions.GetMergedStateOrNull(mutStruct, alreadyExists.State) ??
                                  throw TicErrors.CannotSetState(alreadyExists, mutStruct);
        }
    }

    public static void SetStructInit(this GraphBuilder b, string[] fieldNames, int[] fieldExpressionIds, int id) {
        var fields = new Dictionary<string, TicNode>(fieldNames.Length, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < fieldNames.Length; i++)
        {
            if (fields.ContainsKey(fieldNames[i]))
                throw TicErrors.CannotSetState(
                    b.GetOrCreateNode(id),
                    new StateStruct(fields, false));
            fields.Add(fieldNames[i], b.GetOrCreateNode(fieldExpressionIds[i]));
        }

        b.GetOrCreateStructNode(id, new StateStruct(fields, false));
    }

    /// <summary>Ancestor type constraint on a struct-init node (named-type constructors).</summary>
    public static void SetStructInitType(this GraphBuilder b, int structNodeId, ITicNodeState ancestorType) {
        if (ancestorType is not ITypeState typeState)
            return;
        var structNode = b.GetOrCreateNode(structNodeId);
        var ancestorNode = b.CreateVarType(typeState);
        structNode.AddAncestor(ancestorNode);
        // Stamp literal with ancestor's TypeName so conversion produces NamedStructOf —
        // anonymous expansion would lose the identity needed for F-bound Fit checks.
        if (ancestorType is StateStruct ancStruct && ancStruct.TypeName != null
            && structNode.State is StateStruct litStruct && litStruct.TypeName == null) {
            litStruct.TypeName = ancStruct.TypeName;
        }
        // Eagerly insert the T ≤ opt(T) lift as a graph-level edge between literal and ancestor:
        // WrapAncestorInOptional refuses to mutate solved SyntaxNode literal state at runtime.
        if (ancestorType is StateStruct ancNamed
            && structNode.State is StateStruct lit
            && ancNamed.IsOptionalFieldOrNone()) {
            WrapNonOptionalCompositeLiteralFields(b, lit, ancNamed);
        }
    }

    /// <summary>True iff <paramref name="s"/> has at least one Optional field.</summary>
    private static bool IsOptionalFieldOrNone(this StateStruct s) {
        foreach (var f in s.Fields) {
            if (f.Value.GetNonReference().State is StateOptional) return true;
        }
        return false;
    }

    /// <summary>
    /// For each ancestor-Optional field paired with a non-Optional composite literal value,
    /// insert <c>opt(litValueNode)</c> wrapper. Primitives lift naturally; only solved composite
    /// literals trip WrapAncestorInOptional.
    /// </summary>
    private static void WrapNonOptionalCompositeLiteralFields(GraphBuilder b, StateStruct lit, StateStruct anc) {
        foreach (var ancField in anc.Fields) {
            if (ancField.Value.GetNonReference().State is not StateOptional) continue;
            var litFieldNode = lit.GetFieldOrNull(ancField.Key);
            if (litFieldNode == null) continue;
            var litFieldNr = litFieldNode.GetNonReference();
            if (litFieldNr.State is StateOptional) continue;
            if (litFieldNr.State is StatePrimitive { Name: PrimitiveTypeName.None }) continue;
            // Only composite literals trip WrapAncestorInOptional; primitives lift naturally.
            if (litFieldNr.State is not ICompositeState) continue;
            // Wrapper opt(litFieldNode) sits between the untouched literal and the ancestor field.
            var wrapper = b.CreateVarType(StateOptional.Of(litFieldNode));
            wrapper.IsOptionalElement = false;
            litFieldNode.IsOptionalElement = true;
            lit.ReplaceField(ancField.Key, wrapper);
        }
    }

    public static void SetCompareChain(this GraphBuilder b, int nodeOrderNumber, StateRefTo[] generics, int[] ids) {
        for (int i = 0; i < generics.Length; i++)
        {
            var generic = generics[i];
            b.SetCallArgument(generic, ids[i]);
            b.SetCallArgument(generic, ids[i + 1]);
        }

        b.SetOrCreatePrimitive(nodeOrderNumber, StatePrimitive.Bool);
    }

    /// <summary>Type narrowing: <c>narrowedName: T</c> where <c>originalName: opt(T)</c> (e.g. inside <c>if x != none</c>).</summary>
    public static void SetNarrowedVariable(this GraphBuilder b, string originalName, string narrowedName) {
        var originalNode = b.GetNamedNode(originalName);
        var narrowedNode = b.GetNamedNode(narrowedName);
        var elementNode = b.CreateVarType();
        SolvingFunctions.MergeInplace(elementNode, narrowedNode);
        var optNode = b.CreateVarType(StateOptional.Of(elementNode));
        SolvingFunctions.MergeInplace(optNode, originalNode);
    }
}
