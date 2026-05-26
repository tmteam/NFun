using System;
using System.Collections.Generic;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;

namespace NFun.Tic;

/// <summary>
/// Convenience methods that compose core GraphBuilder operations
/// into NFun language constructs (constants, arrays, structs, lambdas, etc.)
/// </summary>
public static class GraphBuilderExtensions {

    public static void SetVar(this GraphBuilder b, string name, int node) {
        var namedNode = b.GetNamedNode(name);
        var idNode = b.GetOrCreateNode(node);
        if (idNode.State is ConstraintsState)
        {
            // When the named variable has a concrete function type (from SetVarType),
            // use RefTo so SetCall sees the StateFun directly — no scan needed.
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

    /// <summary>
    /// Bind a TIC node to an Optional-typed constant. Used for ConstantSyntaxNode
    /// whose OutputType is `T?` — e.g. a parameter default `x: int? = 5` produces
    /// a ConstantSyntaxNode with value 5 but OutputType Int32?. Without this, the
    /// caller throws "Complex constant type is not supported" (BugHunt-stmt #58).
    /// </summary>
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
        //expr<=returnType<= ...
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
    /// Variant that accepts an optional pre-resolved element type. When the caller knows all
    /// array elements share a named-struct type (e.g. `[t{}, t{}]` where each element's
    /// post-elaboration OutputType is `NamedStructOf("t")`), passing the resolved TIC state
    /// lets the element-LCA node start with the full named recursive shape — instead of an
    /// empty ConstraintsState that absorbs only the literal's raw post-Pull state.
    ///
    /// Without this hint, a single-element array `[t{}]` infers `arr:{...;next:none}[]`
    /// rather than `arr:t[]`: the literal's `next` field stays as `StatePrimitive.None`
    /// (the "None desc → skip" rule preserves None rather than lifting it through the named
    /// ancestor) and the element-LCA node, having no other input, adopts that raw shape.
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

    public static void SetFieldAccess(this GraphBuilder b, int structNodeId, int opId, string fieldName,
        string sourceTypeNameHint = null) {
        var node = b.GetOrCreateStructNode(structNodeId, new StateStruct(isOpen: true))
            .GetNonReference();

        var state = (StateStruct)node.State;
        // Propagate the source variable's named-struct TypeName onto the
        // synthesized open struct. LCA(arr(named-t), arr(anonymous-{...})) drops to anonymous
        // because LcaTypeName is strict (one null → null); stamping the open struct's
        // TypeName from the source's named identity makes both LCA inputs named, so
        // LcaTypeName(t,t)=t survives downstream and ThrowIfRecursiveTypeDefinition's
        // named-type cycle-rescue can repair the contractive μ-cycle.
        if (state.TypeName == null && sourceTypeNameHint != null)
            state.TypeName = sourceTypeNameHint;
        var memberNode = state.GetFieldOrNull(fieldName);
        if (memberNode == null)
        {
            memberNode = b.CreateVarType();
            state.AddField(fieldName, memberNode);
            node.State = state;
        }

        // For named type constructors: check if an ancestor declares this field as Optional.
        // If so, use the ancestor's field node to preserve the declared type.
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
    /// x?.field where x: opt(struct{field: T}) → result: opt(T)
    /// When T is already optional (e.g. int?), result becomes opt(opt(int))
    /// which TIC flattens to opt(int) during Destruction/Finalization.
    /// The runtime SafeFieldAccessExpressionNode also handles this by not
    /// double-wrapping optional field types.
    /// </summary>
    public static void SetSafeFieldAccess(this GraphBuilder b, int sourceNodeId, int opId, string fieldName) {
        // `?.` creates an IsOptionalSourced struct that participates in Push
        // width-propagation and can close a μ-cycle. Mark the graph so
        // cycle-aware passes know they may have work to do.
        b.IsRecursion = true;
        // T — the field's type variable (shared between source and result constraints)
        var fieldTypeNode = b.CreateVarType();

        // struct{fieldName: T}, opt-sourced (the closing edge originates from an
        // Optional constructor — Push width-propagation reads this flag to
        // restore the Optional break when a self-closing cycle would otherwise
        // produce a non-contractive struct→struct loop).
        var fields = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase) { { fieldName, fieldTypeNode } };
        var structState = new StateStruct(fields, isFrozen: false, isOpen: true) { IsOptionalSourced = true };
        var structNode = b.CreateVarType(structState);

        // Source type: opt(struct{fieldName: T})
        var sourceType = StateOptional.Of(structNode);

        // Result type: opt(T)
        var resultType = StateOptional.Of(fieldTypeNode);

        // Use SetCall: [arg_type, return_type], [arg_id, return_id]
        b.SetCall(
            new ITicNodeState[] { sourceType, resultType },
            new[] { sourceNodeId, opId });
    }

    /// <summary>
    /// x?[i] where x: opt(arr(T)) → result: opt(T)
    ///
    /// Built as a single connected subgraph mirroring SetSafeFieldAccess and
    /// SetSafeMethodCall: result.State = StateOptional.Of(elemNode) is set
    /// directly at graph-construction time, BEFORE any Pull/Push runs. This
    /// ensures subsequent constraint edges (e.g. `.count()` adding a
    /// `result →c arr(T')` ancestor) see the result as concretely Optional
    /// from the start — no LCA-with-None pattern that loses IsOptional when
    /// the inferred elem type is itself a composite. (MR6Bug2.)
    ///
    /// Previously: LCA-with-None pattern (`elemNode → result`, `None → result`)
    /// produced bare composite state on result when elem type was an array,
    /// silently dropping the Optional layer through `TransformToArrayOrNull`
    /// during downstream Pull. The directly-built Optional avoids that path
    /// entirely.
    ///
    /// Flattening of opt(opt(T)) when T is itself optional is handled by the
    /// existing FlattenNestedOptional mechanism during Destruction — same as
    /// SetSafeFieldAccess and SetSafeMethodCall.
    /// </summary>
    public static void SetSafeArrayAccess(this GraphBuilder b, int sourceNodeId, int indexNodeId, int resultNodeId) {
        // T — the array element type variable
        var elemNode = b.CreateVarType();

        // Source: opt(arr(T))
        var arrNode = b.CreateVarType(StateArray.Of(elemNode));
        var sourceType = StateOptional.Of(arrNode);
        b.SetCallArgument(sourceType, sourceNodeId);

        // Index: I32
        b.SetCallArgument(StatePrimitive.I32, indexNodeId);

        // Result state = opt(elemNode). Setting concretely upfront — elemNode
        // is shared with the source's array shape, so once Pull resolves the
        // source's actual element type into elemNode, the result Optional
        // covers it directly. None from the source flows in via the standard
        // None ≤ Opt(T) rule on the source-edge, not via a separate noneNode.
        var resultNode = b.GetOrCreateNode(resultNodeId);
        var resultType = StateOptional.Of(elemNode);
        resultNode.State = SolvingFunctions.GetMergedStateOrNull(resultNode.State, resultType)
                           ?? throw TicErrors.CannotSetState(resultNode, resultType);
    }

    /// <summary>
    /// x?.field(args) where x: opt(struct{field: (P1..Pn) → R}) → result: opt(R)
    ///
    /// Built as a single connected subgraph (one TIC special form) instead of three
    /// independent constraint setups (unwrap, field-access, call). The three-stage
    /// version relied on topological Pull threading a chain of edges (opt→struct→fun)
    /// after the descendants were already finalized — those edges never re-pulled and
    /// the function's concrete return type was lost, producing y:Any?. (MR5Bug7.)
    ///
    /// All nodes here are constructed as descendants of structNode in toposort
    /// (structNode → funNode → params/return), so a single Pull cascade from the
    /// source's concrete opt(struct{...}) state flows the field-function's actual
    /// arg and return types into the call's argument and result slots.
    /// </summary>
    public static void SetSafeMethodCall(
        this GraphBuilder b, int sourceNodeId, int[] callArgIds, int resultNodeId, string fieldName) {
        // Same cycle-rescue marker as SetSafeFieldAccess — anonymous-struct + fn-field
        // closures can produce μ-cycles through safe-method-call (MR5Bug4 family).
        b.IsRecursion = true;

        // Param nodes Pi for each call arg; return node R. The return node is shared
        // between the function shape and the result Optional — that direct sharing is
        // what makes Pull cleanly propagate the source's concrete function return type
        // into the result during Destruction (mirrors SetSafeFieldAccess where the
        // field node is shared between struct shape and result Optional).
        var paramNodes = new TicNode[callArgIds.Length];
        for (int i = 0; i < callArgIds.Length; i++)
            paramNodes[i] = b.CreateVarType();
        var returnNode = b.CreateVarType();

        // Field shape: a function (P1..Pn) → R, carried as a single composite node.
        var funNode = b.CreateVarType(StateFun.Of(paramNodes, returnNode));

        // Struct shape: open row-poly {fieldName: funNode}, opt-sourced (a `?.` emission).
        var fields = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase) { { fieldName, funNode } };
        var structState = new StateStruct(fields, isFrozen: false, isOpen: true) { IsOptionalSourced = true };
        var structNode = b.CreateVarType(structState);

        // source ≤ opt(struct{fieldName: (P1..Pn) → R}) — single subtype edge that
        // Pull/Destruction threads from the source's concrete opt(struct{...}) all the
        // way to (P1..Pn)→R, sharing field-function nodes with the result.
        b.SetCallArgument(StateOptional.Of(structNode), sourceNodeId);

        // Each call argᵢ ≤ Pᵢ — call args fit param types (covariant flow at the call site).
        for (int i = 0; i < callArgIds.Length; i++)
            b.SetCallArgument(new StateRefTo(paramNodes[i]), callArgIds[i]);

        // Result state = opt(returnNode). The returnNode is shared with funNode's return
        // slot, so once the source's actual function-typed field resolves through Pull,
        // returnNode inherits the concrete return type (e.g. I32) and result = opt(I32).
        // A None source contributes via the standard None ≤ opt(T) rule on the
        // source-edge — no separate noneNode needed at the result. (Previous LCA-with-None
        // attempt at the result site created a self-reference V2 ↔ result when V2 was
        // unresolved, expanding during Destruction into opt(opt(opt(...))) → Any?.)
        var resultNode = b.GetOrCreateNode(resultNodeId);
        var resultType = StateOptional.Of(returnNode);
        resultNode.State = SolvingFunctions.GetMergedStateOrNull(resultNode.State, resultType)
                           ?? throw TicErrors.CannotSetState(resultNode, resultType);
    }

    /// <summary>
    /// left ?? right: (opt(U), V) → LCA(U, V)
    /// Unwraps left Optional element U, computes LCA(U, right) as result.
    /// Supports optional right: int? ?? int? → int? (LCA(int, int?) = int?).
    /// </summary>
    public static void SetCoalesce(this GraphBuilder b, int leftId, int rightId, int resultId) {
        // U — unwrapped element of left Optional
        var elemNode = b.CreateVarType();

        // Left: opt(U)
        var leftType = StateOptional.Of(elemNode);
        b.SetCallArgument(leftType, leftId);

        // Result = LCA(U, right) — both are subtypes of result
        var resultNode = b.GetOrCreateNode(resultId);
        elemNode.AddAncestor(resultNode);
        var rightNode = b.GetOrCreateNode(rightId);
        rightNode.AddAncestor(resultNode);
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

    /// <summary>
    /// Sets an ancestor type constraint on a struct init node.
    /// Used for named type constructors so TIC knows field types at any nesting depth.
    /// </summary>
    public static void SetStructInitType(this GraphBuilder b, int structNodeId, ITicNodeState ancestorType) {
        if (ancestorType is not ITypeState typeState)
            return;
        var structNode = b.GetOrCreateNode(structNodeId);
        var ancestorNode = b.CreateVarType(typeState);
        structNode.AddAncestor(ancestorNode);
        // When the struct literal corresponds to a NAMED type constructor (ancestor is a
        // StateStruct with TypeName), stamp the literal's state with the same TypeName so
        // subsequent conversion to FunnyType produces NamedStructOf at runtime, preserving
        // identity for F-bound Fit checks. Otherwise the literal stays anonymous and converts to
        // a structurally-expanded Struct, losing the identity needed for runtime dispatch.
        if (ancestorType is StateStruct ancStruct && ancStruct.TypeName != null
            && structNode.State is StateStruct litStruct && litStruct.TypeName == null) {
            litStruct.TypeName = ancStruct.TypeName;
        }
        // When the ancestor declares a field as Optional but the literal's
        // corresponding field-value is a non-Optional composite (struct/array literal), the
        // implicit lift `T ≤ opt(T)` is invoked at Push/Destruction time as
        // WrapAncestorInOptional, which refuses to mutate SyntaxNode literal state and throws.
        // Eagerly *insert* an Optional wrapper node between the literal and the ancestor at
        // graph-construction time, leaving the literal solved and untouched. This converts the
        // implicit-lift postulate into an explicit graph-level edge — exactly once per
        // syntactic field-init boundary, with no runtime detection cost.
        if (ancestorType is StateStruct ancNamed
            && structNode.State is StateStruct lit
            && ancNamed.IsOptionalFieldOrNone()) {
            // (placeholder — helper below). Per-field wrap.
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
    /// For each field of <paramref name="lit"/> where the matching ancestor field declares
    /// <see cref="StateOptional"/>: if the literal's field-value node is a non-Optional
    /// composite (struct/array) literal, insert a fresh TIC node holding
    /// <c>StateOptional.Of(litValueNode)</c> and replace the field link. Skips
    /// already-Optional values, None primitives, and non-composite values (primitives can
    /// be lifted by existing TIC algebra — only solved composite literals are problematic).
    /// </summary>
    private static void WrapNonOptionalCompositeLiteralFields(GraphBuilder b, StateStruct lit, StateStruct anc) {
        foreach (var ancField in anc.Fields) {
            if (ancField.Value.GetNonReference().State is not StateOptional) continue;
            var litFieldNode = lit.GetFieldOrNull(ancField.Key);
            if (litFieldNode == null) continue;
            var litFieldNr = litFieldNode.GetNonReference();
            // Already Optional or None: nothing to do.
            if (litFieldNr.State is StateOptional) continue;
            if (litFieldNr.State is StatePrimitive { Name: PrimitiveTypeName.None }) continue;
            // Only wrap composite (struct/array/fun) literals — they are the cases where
            // WrapAncestorInOptional throws. Primitives/CS lift naturally via T ≤ opt(T).
            if (litFieldNr.State is not ICompositeState) continue;
            // Build wrapper: opt(litFieldNode). The literal node is untouched; the wrapper
            // is what the ancestor's Optional field gets compared against.
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

    /// <summary>
    /// Sets up type narrowing: narrowedName has type T where originalName has type opt(T).
    /// Used for type narrowing in if-then-else branches where x != none guarantees non-optional.
    /// </summary>
    public static void SetNarrowedVariable(this GraphBuilder b, string originalName, string narrowedName) {
        var originalNode = b.GetNamedNode(originalName);
        var narrowedNode = b.GetNamedNode(narrowedName);
        // T — the unwrapped element type, merged with the narrowed variable
        var elementNode = b.CreateVarType();
        SolvingFunctions.MergeInplace(elementNode, narrowedNode);
        // original = opt(T): merge original with opt(T)
        var optNode = b.CreateVarType(StateOptional.Of(elementNode));
        SolvingFunctions.MergeInplace(optNode, originalNode);
    }
}
