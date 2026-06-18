using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;

namespace NFun.Tic;

public class GraphBuilder {
    private readonly Dictionary<string, TicNode> _variables = new(StringComparer.OrdinalIgnoreCase);
    private TicNode[] _syntaxNodes;
    private int _syntaxNodesLength;
    private readonly List<TicNode> _typeVariables = new();
    private int _varNodeId = 0;
    private readonly List<TicNode> _outputNodes = new();
    private readonly List<TicNode> _inputNodes = new();

    /// <summary>Registry of named type definitions for lazy expansion of recursive types.</summary>
    internal Types.INamedTypeFieldRegistry _namedTypeRegistry;
    internal Types.INamedTypeFieldRegistry NamedTypeRegistry {
        get => _namedTypeRegistry;
        set {
            _namedTypeRegistry = value;
            // Named types are the only entry point for declared μ-recursive
            // shapes (struct {next: self?}). Their presence indicates that
            // downstream cycle-aware passes may have work to do; absence
            // proves they don't.
            if (value != null) IsRecursion = true;
        }
    }

    /// <summary>
    /// Set to true when the graph contains any construct that could produce
    /// a μ-recursive type: a named-type registry (declared recursive shapes),
    /// a SafeFieldAccess (`?.` — creates IsOptionalSourced struct that can
    /// close cycles via Push), or a user function (potential self-recursion
    /// via SetCall(StateFun) — its body may contain a recursive call).
    /// All cycle-aware destruction-time machinery (ThrowIfRecursiveType-
    /// Definition's pre-scan, LiftMuTypes, ScCClosurePass) early-exits
    /// when this is false. Non-recursive expressions thus pay zero cycle-
    /// detection cost.
    /// </summary>
    public bool IsRecursion { get; set; }

    public GraphBuilder() { _syntaxNodes = new TicNode[16]; _syntaxNodesLength = 16; }
    public GraphBuilder(int maxSyntaxNodeId) { _syntaxNodes = new TicNode[maxSyntaxNodeId]; _syntaxNodesLength = maxSyntaxNodeId; }

    public StateRefTo InitializeVarNode(ITypeState desc = null, StatePrimitive anc = null, bool isComparable = false)
        => new(CreateVarType(ConstraintsState.Of(desc, anc, isComparable)));

    /// <summary>
    /// Stage C.4a — emit a generic var node carrying a <see cref="StateCompositeConstraints"/>
    /// instead of a plain <see cref="ConstraintsState"/>. Used by signatures with composite-shape
    /// constraints (<c>Enumerable&lt;T&gt;</c>, <c>IndexedRead&lt;T&gt;</c>, etc.) so that Pull
    /// from concrete <see cref="StateCollection"/> arguments refines the CompCS interval per
    /// <c>Specs/Tic/Algebra_CompositeConstraints.md</c> §4.1.1.
    /// </summary>
    public StateRefTo InitializeCompositeVarNode(ConstructorKind? ancestor) {
        var elementNode = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        var compcs = StateCompositeConstraints.Create(
            elementNode,
            ancestor: ancestor,
            descendant: null,
            isOptional: false);
        return new StateRefTo(CreateVarType(compcs));
    }

    #region node management

    /// <summary>
    /// Returns already exists syntax node, or creates new one with empty constraints
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TicNode GetOrCreateNode(int id) {
        if (id >= _syntaxNodesLength) GrowSyntaxNodes(id);
        var alreadyExists = _syntaxNodes[id];
        if (alreadyExists != null)
            return alreadyExists;

        var res = TicNode.CreateSyntaxNode(id, ConstraintsState.Empty, true);
        _syntaxNodes[id] = res;
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TicNode GetSyntaxNodeOrEnlarge(int id) {
        if (id >= _syntaxNodesLength) GrowSyntaxNodes(id);
        return _syntaxNodes[id];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowSyntaxNodes(int id) {
        var newLen = Math.Max(id + 1, _syntaxNodesLength * 2);
        var newArr = new TicNode[newLen];
        Array.Copy(_syntaxNodes, newArr, _syntaxNodesLength);
        _syntaxNodes = newArr;
        _syntaxNodesLength = newLen;
    }

    public TicNode GetNamedNode(string name) {
        if (_variables.TryGetValue(name, out var varnode))
            return varnode;

        var ans = TicNode.CreateNamedNode(name, ConstraintsState.Empty);
        _variables.Add(name, ans);
        return ans;
    }

    /// <summary>
    /// Lookup-only variant of <see cref="GetNamedNode"/>. Returns the existing
    /// variable node if present, or null. Does NOT create or register a new
    /// node — used for pre-Solve introspection where the absence of a known
    /// variable is meaningful (e.g. annotation-check before adding constraints).
    /// </summary>
    public TicNode GetNamedNodeOrNull(string name)
        => _variables.TryGetValue(name, out var varnode) ? varnode : null;

    public TicNode[] GetNamedNodes(string[] names) {
        var ans = new TicNode[names.Length];
        for (int i = 0; i < names.Length; i++)
            ans[i] = GetNamedNode(names[i]);
        return ans;
    }

    public bool HasNamedNode(string s) => _variables.ContainsKey(s);

    public TicNode CreateVarType(ITicNodeState state = null) {
        if (state is ICompositeState composite)
            RegistrateCompositeType(composite);

        var varNode = TicNode.CreateTypeVariableNode(
            name: "V" + _varNodeId,
            state: state ?? ConstraintsState.Empty,
            true);
        _varNodeId++;
        _typeVariables.Add(varNode);
        return varNode;
    }

    /// <summary>
    /// Merge already exists syntax node, or creates new one with specified type
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MergeOrSetNode(int id, StateRefTo type) {
        var alreadyExists = GetSyntaxNodeOrEnlarge(id);
        if (alreadyExists == null)
        {
            var res = TicNode.CreateSyntaxNode(id, type, true);
            _syntaxNodes[id] = res;
            return;
        }

        alreadyExists.State = SolvingFunctions.GetMergedStateOrNull(alreadyExists.State, type) ??
                              throw TicErrors.CannotSetState(alreadyExists, type);
    }

    public void SetOrCreatePrimitive(int id, StatePrimitive type) {
        var node = GetOrCreateNode(id);
        if (!node.TryBecomeConcrete(type))
            throw TicErrors.CannotSetState(node, type);
    }

    public void GetOrCreateArrayNode(int id, TicNode elementType) {
        var alreadyExists = GetSyntaxNodeOrEnlarge(id);
        if (alreadyExists != null)
        {
            alreadyExists.State =
                SolvingFunctions.GetMergedStateOrNull(new StateArray(elementType), alreadyExists.State) ??
                throw TicErrors.CannotSetState(elementType, new StateArray(elementType));
            return;
        }

        var res = TicNode.CreateSyntaxNode(id, new StateArray(elementType), true);
        _syntaxNodes[id] = res;
    }

    /// <summary>
    /// Lang-mode sibling of <see cref="GetOrCreateArrayNode"/>: registers a syntax node as
    /// <see cref="StateCollection"/> with <see cref="ConstructorKind.List"/>.
    /// </summary>
    public void GetOrCreateListNode(int id, TicNode elementType) {
        var newState = StateCollection.OfList(elementType);
        var alreadyExists = GetSyntaxNodeOrEnlarge(id);
        if (alreadyExists != null)
        {
            alreadyExists.State =
                SolvingFunctions.GetMergedStateOrNull(newState, alreadyExists.State) ??
                throw TicErrors.CannotSetState(elementType, newState);
            return;
        }

        var res = TicNode.CreateSyntaxNode(id, newState, true);
        _syntaxNodes[id] = res;
    }

    /// <summary>
    /// Lang-mode mutable array — sibling of <see cref="GetOrCreateListNode"/>.
    /// Registers a syntax node as <see cref="StateCollection"/> with
    /// <see cref="ConstructorKind.Array"/>.
    /// </summary>
    public void GetOrCreateMutableArrayNode(int id, TicNode elementType) {
        var newState = StateCollection.OfMutableArray(elementType);
        var alreadyExists = GetSyntaxNodeOrEnlarge(id);
        if (alreadyExists != null)
        {
            alreadyExists.State =
                SolvingFunctions.GetMergedStateOrNull(newState, alreadyExists.State) ??
                throw TicErrors.CannotSetState(elementType, newState);
            return;
        }

        var res = TicNode.CreateSyntaxNode(id, newState, true);
        _syntaxNodes[id] = res;
    }

    public TicNode GetOrCreateStructNode(int id, StateStruct stateStruct) {
        var alreadyExists = GetSyntaxNodeOrEnlarge(id);
        if (alreadyExists != null)
        {
            alreadyExists.State = SolvingFunctions.GetMergedStateOrNull(stateStruct, alreadyExists.State) ??
                                  throw TicErrors.CannotSetState(alreadyExists, stateStruct);
            return alreadyExists;
        }

        var res = TicNode.CreateSyntaxNode(id, stateStruct, true);
        _syntaxNodes[id] = res;
        return res;
    }

    public void SetOrCreateLambda(int lambdaId, TicNode[] args, TicNode ret) {
        var fun = StateFun.Of(args, ret);

        var alreadyExists = GetSyntaxNodeOrEnlarge(lambdaId);
        if (alreadyExists != null)
        {
            alreadyExists.State = SolvingFunctions.GetMergedStateOrNull(fun, alreadyExists.State) ??
                                  throw TicErrors.CannotSetState(alreadyExists, fun);
        }
        else
        {
            var res = TicNode.CreateSyntaxNode(lambdaId, fun, true);
            _syntaxNodes[lambdaId] = res;
        }
    }

    /// <summary>
    /// Optimized version of SetCallArgument for ref cases.
    /// When the arg is a fresh constraint node (e.g. integer constant)
    /// whose range is fully subsumed by the generic's range,
    /// converts it to a direct reference — avoids an ancestor edge
    /// and simplifies solver work.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCallArgument(StateRefTo type, int argId) {
        var node = GetOrCreateNode(argId);
        // Fast check: only fresh nodes (no ancestors) can be const-ref candidates.
        // Keeps the method small for JIT inlining — slow path is NoInlining.
        if (node.Ancestors.Count == 0 && TryConvertConstToRef(node, type))
            return;
        node.AddAncestor(type.Node);
    }

    /// <summary>
    /// Slow path: checks if a fresh constraint node's range is fully subsumed
    /// by the generic's range, and if so, converts it to a direct reference.
    /// Common case: integer constant (2) as arg to arithmetic op (x * 2).
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TryConvertConstToRef(TicNode node, StateRefTo type) {
        if (node.IsMemberOfAnything)
            return false;
        if (node.State is not ConstraintsState argCs || !argCs.HasDescendant)
            return false;
        if (type.Node.State is not ConstraintsState genCs)
            return false;
        // Check: generic's range [genDesc..genAnc] ⊆ arg's range [argDesc..argAnc]
        if (genCs.Descendant is not StatePrimitive genDesc) return false;
        if (argCs.Descendant is not StatePrimitive argDesc) return false;
        if (genCs.Ancestor == null || argCs.Ancestor == null) return false;
        if (!argDesc.CanBePessimisticConvertedTo(genDesc)) return false;
        if (!genCs.Ancestor.CanBePessimisticConvertedTo(argCs.Ancestor)) return false;
        if (argCs.IsComparable && !genCs.IsComparable) return false;
        // Transfer preferred from constant to generic (so type resolution is preserved)
        if (argCs.Preferred != null && genCs.Preferred == null)
            genCs.Preferred = argCs.Preferred;
        node.State = type;
        return true;
    }

    public void SetCallArgument(ITicNodeState type, int argId) {
        var node = GetOrCreateNode(argId);
        switch (type)
        {
            case StatePrimitive primitive:
            {
                if (node.State is ConstraintsState { HasAncestor: false, HasDescendant: false })
                {
                    node.State = type;
                    return;
                }

                if (!node.TrySetAncestor(primitive))
                    throw TicErrors.CannotSetState(node, primitive);
                break;
            }
            case StateCompositeConstraints compcs:
            {
                // Stage C.5: composite-shape generic signature param (e.g. Enumerable<T>).
                // The CompCS carries its own ElementNode which must be registered alongside.
                if (compcs.ElementNode.State is ICompositeState elemComp)
                    RegistrateCompositeType(elemComp);
                if (!compcs.ElementNode.Registered) {
                    compcs.ElementNode.Registered = true;
                    _typeVariables.Add(compcs.ElementNode);
                }
                var ancestor = CreateVarType(compcs);
                ancestor.IsSignatureParam = true;
                node.AddAncestor(ancestor);
                break;
            }
            case ICompositeState composite:
            {
                RegistrateCompositeType(composite);
                var ancestor = CreateVarType(composite);
                // Function signature params have fixed composite shape.
                // WrapAncestorInOptional checks this flag and throws TIC error.
                // Algebraic meaning: Opt(T) ≤ T is invalid — param shape is a given, not inferred.
                ancestor.IsSignatureParam = true;
                node.AddAncestor(ancestor);
                break;
            }
            case StateRefTo refTo:
            {
                // Guard against self-loop: if the call arg already references the same
                // target node, adding it as ancestor would create a trivial cycle
                // (the toposort ref-transfer would produce node.AddAncestor(node)).
                // This occurs in recursive calls that pass a parameter unchanged.
                var target = refTo.Node.GetNonReference();
                if (node.State is StateRefTo existingRef && existingRef.Node.GetNonReference() == target)
                    break;
                node.AddAncestor(target);
                break;
            }
            default: throw new NotSupportedException();
        }
    }

    private void RegistrateCompositeType(ICompositeState composite) {
        for (int mi = 0; mi < composite.MemberCount; mi++)
        {
            var member = composite.GetMember(mi);
            if (!member.Registered)
            {
                member.Registered = true;
                if (member.State is ICompositeState c)
                    RegistrateCompositeType(c);
                _typeVariables.Add(member);
            }
        }
    }

    #endregion

    #region definitions and calls

    public void SetVarType(string s, ITicNodeState state) {
        if (!TrySetVarType(s, state))
            throw new InvalidOperationException();
    }

    public bool TrySetVarType(string s, ITicNodeState state) {
        var node = GetNamedNode(s);
        switch (state)
        {
            case StatePrimitive primitive:
                return node.TryBecomeConcrete(primitive);
            case ICompositeState composite:
                // Composite re-annotation on an already-solved node would
                // trip the TicNode state-setter assertion "Node is already
                // solved" — surface it as a clean FU879
                // "Variable is already declared" via the caller. (Round 6 #83.)
                if (!node.IsMutable && !state.Equals(node.State))
                    return false;
                RegistrateCompositeType(composite);
                node.State = state;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Constrains expression node to be assignable to the named variable's type.
    /// Like SetDef but without marking the variable as output.
    /// Used for default value expressions in function parameters.
    /// </summary>
    public void SetDefaultValueConstraint(string varName, int exprNodeId) {
        var exprNode = GetOrCreateNode(exprNodeId);
        var varNode = GetNamedNode(varName);
        TraceLog.WriteLine($"  SetDefaultValueConstraint: {exprNodeId}({exprNode.State}).AddAncestor({varName}({varNode.State}))");
        exprNode.AddAncestor(varNode);
    }

    public void SetDef(string name, int rightNodeId) {
        var exprNode = GetOrCreateNode(rightNodeId);
        var defNode = GetNamedNode(name);
        if (!_outputNodes.Contains(defNode))
            _outputNodes.Add(defNode);

        if (exprNode.State is StatePrimitive primitive && defNode.State is ConstraintsState constrains)
            constrains.Preferred = primitive;

        // Bug hunt round 9 #50. Identity-share when binding a rule literal
        // (StateFun) to an unannotated, unconstrained variable. Without this,
        // SetCall on `defNode` later hits the ELSE branch at line 721-733,
        // synthesizing a FRESH StateFun whose generic arg nodes lack the
        // rule's declared rigidity. Call-site contravariant Pull then narrows
        // them to the call literal's type (e.g. `array<I32>` declared at rule,
        // but call passes `list<I32>` → f's slot widens to `(list<I32>)->R`).
        // After identity-share, SetCall("f", …) finds StateFun directly and
        // routes via line 714 → 463-469, which sets IsSignatureParam on the
        // composite arg nodes carried from the rule's declared annotation,
        // preserving the declared signature through the call.
        // The Pull edge `exprNode → defNode` must be SKIPPED here because
        // Pull's Apply(StateFun, StateFun) on identity-shared StateFun would
        // call `lambdaFun.RetNode.AddAncestor(lambdaFun.RetNode)` — a self-
        // loop the AddAncestor guard rejects. The shared state already
        // realizes the binding equivalence; no additional edge is needed.
        // Scoped: rule-literal binding only — primitives, collections, and
        // user-fn dispatch flows go through unrelated paths.
        if (exprNode.State is StateFun lambdaFun
            && defNode.State is ConstraintsState defCs && defCs.NoConstrains
            && defNode.Ancestors.Count == 0)
        {
            defNode.State = lambdaFun;
            return;
        }

        exprNode.AddAncestor(defNode);
    }

    public StateFun SetFunDef(string name, int returnId, ITypeState returnType = null, params string[] varNames) {
        var args = GetNamedNodes(varNames);
        var exprId = GetOrCreateNode(returnId);
        var returnTypeNode = CreateVarType(returnType);
        //expr<=returnType<= ...
        exprId.AddAncestor(returnTypeNode);
        var fun = StateFun.Of(args, returnTypeNode);

        var node = GetNamedNode(name);
        if (node.State is not ConstraintsState c || !c.NoConstrains)
            throw new InvalidOperationException($"variable {name}already declared");
        node.State = fun;
        _outputNodes.Add(returnTypeNode);
        _inputNodes.AddRange(args);
        return fun;
    }

    /// <summary>
    /// Set function call, where function variable (or expression) placed at bodyId
    /// </summary>
    public void SetCall(int bodyId, params int[] argThenReturnIds)
        => SetCall(GetOrCreateNode(bodyId), argThenReturnIds);

    /// <summary>
    /// Set function call, of function variable with id of name
    /// </summary>
    public void SetCall(string name, params int[] argThenReturnIds)
        => SetCall(GetNamedNode(name), argThenReturnIds);

    /// <summary>
    /// Set function call, of already known functional type
    /// </summary>
    public void SetCall(StateFun funState, params int[] argThenReturnIds) {
        if (funState.ArgsCount != argThenReturnIds.Length - 1)
            throw new ArgumentException("Sizes of type and id array have to be equal");

        RegistrateCompositeType(funState);

        // IsRecursion is set by RuntimeBuilder when functionSyntaxNode.IsRecursive
        // (computed by FindFunctionDependenciesVisitor during solve-order
        // analysis, BEFORE TIC). No need to set it heuristically here.

        // Per-call-site instantiation for F-bounded functions. When the function carries an
        // F-bound (CS{StructBound} on any argNode or returnNode), the signature is shared across
        // call sites — without cloning, two sites passing structurally-different types collide on
        // the Descendant slot (LCA → Any). Damas-Milner '82 let-polymorphism: instantiate
        // signature fresh per call.
        // IsRecursion gates the entire μ-machinery: when false, no signature can carry a
        // recursive shape (named-type registry hasn't been set), so skip the walk.
        bool needsPerSiteClone = IsRecursion && SignatureHasRecursiveShape(funState);
        var argNodeMap = needsPerSiteClone
            ? CreatePerSiteCloneMap(funState)
            : null;

        for (int i = 0; i < funState.ArgsCount; i++)
        {
            var argNode = funState.ArgNodes[i];
            // Mark composite params from function signature as pinned —
            // prevents Optional wrapping (Opt(T) ≤ T is invalid for given types).
            if (argNode.State is ICompositeState)
                argNode.IsSignatureParam = true;

            // Per-site clone redirect: use clone of argNode for THIS call's edges.
            // Map keys are GetNonReference forms.
            var argKey = argNode.GetNonReference();
            var effectiveArgNode = argNodeMap != null && argNodeMap.TryGetValue(argKey, out var cloned)
                ? cloned
                : argNode;
            var state = effectiveArgNode.State;
            // Route composite-state and constraint-state args through StateRefTo when sharing
            // would create aliased member TicNodes downstream (Damas-Milner '82 §3
            // let-monomorphic body — each param-use is a reflexive constraint, so the call
            // should add an edge, not a fresh structural copy).
            //
            // Gate:
            // - ConstraintsState: always (Descendant slot may share TicNodes across sites).
            // - StateFun: always — even when solved, function-shaped composites contain
            //   member TicNodes that get shared via SetCallArgument(composite,…)'s
            //   CreateVarType(composite) path; a recursive self-call passing the same
            //   param through would hit "Circular ancestor 0" on AddAncestor.
            // - Other composites (StateArray/StateOptional/StateStruct): only when unsolved.
            //   Solved arrays/structs have terminal member states with no further
            //   constraint propagation, so sharing them is safe and skips an indirection.
            if (state is ConstraintsState
                || state is StateFun
                || (state is ICompositeState comp && !comp.IsSolved))
                state = new StateRefTo(effectiveArgNode);
            SetCallArgument(state, argThenReturnIds[i]);
        }

        var returnId = argThenReturnIds[^1];
        var returnNode = GetOrCreateNode(returnId);
        // When fun's return state contains contractive cycle nodes (F-bound), do NOT MergeInplace —
        // that would either share cycle TicNodes across GraphBuilder instances (corrupting them
        // across solves) or trigger Circular ancestor on SetAncestor. Route the return through
        // StateRefTo to the function's return node so the cycle stays internal to the function's
        // signature; the call site sees a single RefTo edge.
        // Same IsRecursion gate as needsPerSiteClone above — without recursive constructs in
        // the graph, no return position can carry a contractive cycle.
        if (IsRecursion && ReturnContainsContractiveCycle(funState.RetNode))
        {
            var refToFunReturn = new StateRefTo(funState.RetNode);
            if (returnNode.State is ConstraintsState cs && cs.NoConstrains)
                returnNode.State = refToFunReturn;
            else
                SolvingFunctions.MergeInplace(funState.RetNode, returnNode);
        }
        else
        {
            SolvingFunctions.MergeInplace(funState.RetNode, returnNode);
        }
    }

    /// <summary>
    /// Detects whether a function signature contains a μ-recursive cycle (struct with self-RefTo
    /// through fields). Such functions are F-bounded polymorphic and need per-call-site
    /// instantiation to prevent Descendant LCA collision across call sites with
    /// structurally-different types.
    /// </summary>
    private static bool SignatureHasRecursiveShape(StateFun funState) {
        var visited = new HashSet<TicNode>();
        foreach (var arg in funState.ArgNodes)
            if (NodeHasRecursiveShape(arg, visited)) return true;
        return NodeHasRecursiveShape(funState.RetNode, visited);
    }

    private static bool NodeHasRecursiveShape(TicNode n, HashSet<TicNode> visited) {
        var nr = n.GetNonReference();
        if (!visited.Add(nr)) return false;
        if (nr.State is ConstraintsState cs && cs.HasStructBound) return true;
        if (nr.State is StateStruct s && SolvingFunctions.StructIsRecursiveCycle(s, nr)) return true;
        if (nr.State is StateOptional opt) return NodeHasRecursiveShape(opt.ElementNode, visited);
        if (nr.State is ICompositeState composite) {
            for (int i = 0; i < composite.MemberCount; i++)
                if (NodeHasRecursiveShape(composite.GetMember(i), visited)) return true;
        }
        return false;
    }

    /// <summary>
    /// Deep structural clone of the function signature subgraph for THIS call site.
    /// Damas-Milner '82 let-polymorphism: signature is the polymorphic template; each caller
    /// gets a fresh monomorphic instance. Cycle-safe via placeholder pattern: the new node is
    /// registered in the map BEFORE recursing into its state, so back-edges through the cycle
    /// find their clone target and close the cycle in the clone.
    /// </summary>
    private Dictionary<TicNode, TicNode> CreatePerSiteCloneMap(StateFun funState) {
        var map = new Dictionary<TicNode, TicNode>();
        foreach (var arg in funState.ArgNodes)
            DeepCloneNode(arg, map);
        // RetNode is intentionally NOT pre-cloned: SetCall handles return via
        // its own MergeInplace/RefTo path, and cloning here would break the
        // sharing assumption between arg and return positions for monovariant μ.
        return map.Count > 0 ? map : null;
    }

    private TicNode DeepCloneNode(TicNode original, Dictionary<TicNode, TicNode> map) {
        var nr = original.GetNonReference();
        if (map.TryGetValue(nr, out var existing)) return existing;
        // Solved primitive states are monomorphic — share by reference.
        if (nr.State is StatePrimitive) return nr;
        // Solved function shapes ((int)->int etc.) carry no polymorphic carrier:
        // their ArgNodes/RetNode are concrete. Sharing is mandatory — without it,
        // the placeholder would be a distinct TicNode wrapping a CloneState-returned
        // StateFun whose inner nodes still alias the original's. Pull's
        // Apply(StateFun, StateFun) then runs `retNode.AddAncestor(retNode)` on the
        // shared primitive ret and panics "Circular ancestor 0" (BugHunt-stmt #49).
        if (nr.State is StateFun fn && fn.IsSolved) return nr;

        // Placeholder pattern: register the clone BEFORE recursing into state.
        // Any cycle through this node (RefTo → DeepCloneNode → reach this nr again)
        // will find the placeholder in map and use it instead of recursing.
        var placeholder = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        map[nr] = placeholder;
        placeholder.State = CloneState(nr.State, map);
        return placeholder;
    }

    private ITicNodeState CloneState(ITicNodeState state, Dictionary<TicNode, TicNode> map) {
        switch (state) {
            case ConstraintsState cs:
                return cs.GetCopy();
            case StateRefTo r:
                return new StateRefTo(DeepCloneNode(r.Node, map));
            case StateOptional opt:
                return new StateOptional(DeepCloneNode(opt.ElementNode, map));
            case StateArray arr:
                return StateArray.Of(arr.ElementNode); // arrays preserve sharing — element typically primitive
            case StateStruct s when s.TypeName != null:
                // Named types are structurally fixed by their declaration.
                // Sharing is safe: the identity comes from the registry.
                return s;
            case StateStruct s:
                // Anonymous struct (potentially recursive): clone fields
                // through the map to preserve cycles.
                var newFields = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
                foreach (var (name, fn) in s.Fields)
                    newFields[name] = DeepCloneNode(fn, map);
                return new StateStruct(newFields, isFrozen: s.IsFrozen, isOpen: s.IsOpen) {
                    TypeName = s.TypeName,
                    IsOptionalSourced = s.IsOptionalSourced,
                };
            case StateFun funState:
                // Unsolved function shape (polymorphic in arg/ret): clone inner
                // nodes through the map. Solved StateFun is short-circuited by
                // the IsSolved share in DeepCloneNode and never reaches here.
                var clonedArgs = new TicNode[funState.ArgNodes.Length];
                for (int i = 0; i < funState.ArgNodes.Length; i++)
                    clonedArgs[i] = DeepCloneNode(funState.ArgNodes[i], map);
                var clonedRet = DeepCloneNode(funState.RetNode, map);
                return StateFun.Of(clonedArgs, clonedRet);
            default:
                return state;
        }
    }

    private static bool ReturnContainsContractiveCycle(TicNode retNode) {
        var visited = new HashSet<TicNode>();
        return ContainsContractiveCycle(retNode, visited);
    }

    /// <summary>
    /// True iff <paramref name="n"/> reaches itself through a chain of composite members
    /// (μ-recursive type), or sits on a cycle head marked by NodeToposort. Used by SetCall
    /// to decide whether the function's return must route through RefTo (cycle preserved)
    /// or can MergeInplace (no cycle to disturb).
    /// At graph-build time NodeToposort hasn't run yet, so the flag is not enough — we walk
    /// the composite graph and detect back-edges directly. visited is back-tracked so
    /// shared sub-structure (DAG) is not misreported as a cycle.
    /// </summary>
    private static bool ContainsContractiveCycle(TicNode n, HashSet<TicNode> visited) {
        var nr = n.GetNonReference();
        if (nr.IsContractiveCycleHead) return true;
        if (nr.State is not ICompositeState composite) return false;
        if (!visited.Add(nr)) return true; // back-edge through composites → cycle
        bool result = false;
        for (int i = 0; i < composite.MemberCount; i++) {
            if (ContainsContractiveCycle(composite.GetMember(i), visited)) {
                result = true;
                break;
            }
        }
        visited.Remove(nr);
        return result;
    }

    /// <summary>Binary pure generic call (T,T):T — zero array allocation.</summary>
    public void SetCall(StateRefTo generic, int arg0Id, int arg1Id, int returnId) {
        SetCallArgument(generic, arg0Id);
        SetCallArgument(generic, arg1Id);
        MergeOrSetNode(returnId, generic);
    }

    /// <summary>Unary pure generic call (T):T — zero array allocation.</summary>
    public void SetCall(StateRefTo generic, int argId, int returnId) {
        SetCallArgument(generic, argId);
        MergeOrSetNode(returnId, generic);
    }

    /// <summary>
    /// Set pure generic function call
    /// for signatures like (T,T...):T.
    ///
    /// Optimized version of setCall([],[])
    /// </summary>
    public void SetCall(StateRefTo generic, int[] argThenReturnIds) {
        for (int i = 0; i < argThenReturnIds.Length - 1; i++)
            SetCallArgument(generic, argThenReturnIds[i]);

        var returnId = argThenReturnIds[^1];
        //Since we know that the type refers to a generic type,
        // in most case we can immediately create a node with this type.
        MergeOrSetNode(returnId, generic);
    }

    /// <summary>
    /// Set function call, with function signature
    /// </summary>
    public void SetCall(ITicNodeState[] argThenReturnTypes, int[] argThenReturnIds) {
        Debug.Assert(argThenReturnTypes.Length == argThenReturnIds.Length);

        for (int i = 0; i < argThenReturnIds.Length - 1; i++)
            SetCallArgument(argThenReturnTypes[i], argThenReturnIds[i]);

        var returnType = argThenReturnTypes[argThenReturnIds.Length - 1];

        var returnId = argThenReturnIds[^1];
        var returnNode = GetOrCreateNode(returnId);
        returnNode.State = SolvingFunctions.GetMergedStateOrNull(returnNode.State, returnType) ??
                           throw TicErrors.CannotSetState(returnNode, returnType);
    }

    private void SetCall(TicNode functionNode, int[] argThenReturnIds) {
        var id = argThenReturnIds[^1];

        // When functionNode is RefTo(target), the synthesized StateFun must be written
        // onto target.State, not functionNode.State. Overwriting functionNode.State would
        // sever the RefTo link and the target node — which carries the declared field type
        // (e.g. `t: rule()->s?` on a named struct) once Pull propagates from the source —
        // would remain unconstrained, disconnected from the call's fresh args/return.
        var targetNode = functionNode;
        if (functionNode.State is StateRefTo r)
            targetNode = r.Node;
        var state = targetNode.State;

        if (state is StateFun fun)
        {
            if (fun.ArgsCount != argThenReturnIds.Length - 1)
                throw TicErrors.InvalidFunctionalVariableSignature(targetNode);

            SetCall(fun, argThenReturnIds);
        }
        else
        {
            var idNode = GetOrCreateNode(id);

            var genericArgs = new TicNode[argThenReturnIds.Length - 1];
            for (int i = 0; i < argThenReturnIds.Length - 1; i++)
                genericArgs[i] = CreateVarType();

            var newFunVar = StateFun.Of(genericArgs, idNode);
            if (state is not ConstraintsState constrains || !constrains.CanBeConvertedTo(newFunVar))
                throw TicErrors.IsNotAFunctionalVariableOrFunction(targetNode, newFunVar);
            targetNode.State = newFunVar;
            SetCall(newFunVar, argThenReturnIds);
        }
    }

    #endregion

    #region solving

    public ITicResults Solve(bool ignorePrefered = false) => SolveCore(ignorePrefered);

    private ITicResults SolveCore(bool ignorePrefered) {
        // Broadcast the recursion-possibility flag to thread-static so all
        // cycle-aware hot paths (Stages.Invoke visited-pair guard, etc.) can
        // short-circuit. Set by GraphBuilder construction (NamedTypeRegistry,
        // SafeFieldAccess) or explicitly by RuntimeBuilder for recursive
        // user functions (functionSyntaxNode.IsRecursive).
        Stages.StagesExtension._isRecursion = IsRecursion;

        // Check if any None nodes exist (quick scan during AddToTopology).
        // If no None: fused Toposort+Pull (streaming, saves one O(n) pass).
        // If None: separate two-phase Pull needed (IsOptional flag ordering).
        bool hasNone = false;
        for (int i = 0; i < _syntaxNodesLength; i++)
            if (_syntaxNodes[i]?.State == StatePrimitive.None) { hasNone = true; break; }

        bool hasOptionalTypes;
        TicNode[] sorted;
        if (!hasNone) {
            // Fast path: fused Toposort+Pull (no None → single pass)
            sorted = Toposort(node => {
                if (!node.IsMemberOfAnything)
                    SolvingFunctions.PullConstraintsForNode(node);
            });
            hasOptionalTypes = false;
            PrintTrace("1+2. Toposorted+Pulled (fused)", sorted);
        } else {
            // Slow path: separate Toposort then two-phase Pull
            sorted = Toposort();
            PrintTrace("1. Toposorted", sorted);
            SolvingFunctions.PullConstraintsTwoPhase(sorted);
            hasOptionalTypes = true;
            PrintTrace("2. PullConstraints (two-phase)", sorted);
        }

        // Broadcast Preferred BEFORE Push. Push's Apply(StatePrimitive, ConstraintsState)
        // collapses literal CS [U8..Re]I32! to bare U8 when its ancestor pins it to a single
        // primitive (e.g., `y:byte = 5` → literal `5` is forced to U8 by z annotation, losing
        // the I32 Preferred). With Preferred broadcast AFTER Push, no CS carries Preferred and
        // CollectPreferred finds nothing — `byte+byte` and `int16+int16` with negative literals
        // then default-resolve to Real instead of I32. Running PropagatePreferred between Pull
        // and Push captures the literal's Preferred while it still lives on the CS (per
        // TicPreferred.md P3 monotonicity — Preferred is metadata, doesn't affect Pull/Push).
        // (MR2Bug4.)
        SolvingFunctions.PropagatePreferred(sorted);
        SolvingFunctions.PushConstraints(sorted);
        // SCC closure via Kleene fixpoint for cyclic contractive components
        // (Amadio-Cardelli §4.2 / Pottier-Rémy '05 §10.6). Single-pass Push leaves degenerate
        // ref-chains in self-referential return positions of co-recursive functions; iterating
        // Push on each cyclic SCC to fixpoint propagates the F-bound through the cycle, producing
        // the canonical regular tree. Acyclic singletons skip the SCC pass — zero overhead for
        // simple code. Skipped entirely when the graph cannot have μ-recursion.
        if (IsRecursion) ScCClosurePass(sorted);
        PrintTrace("3. PushConstraints", sorted);

        // Pass NamedTypeRegistry through Destruction/Finalize so TryRepairOptSourcedCycle can
        // match cycle structs against declared named types and stamp TypeName on the µ-type root.
        // Threaded explicitly (no globals) so concurrent solves are isolated.
        bool allTypesAreSolved = SolvingFunctions.Destruction(sorted, hasOptionalTypes, NamedTypeRegistry, IsRecursion);
        PrintTrace("4. Destructed");

        if (allTypesAreSolved)
            return new TicResultsWithoutGenerics(_variables, _syntaxNodes);

        var results = SolvingFunctions.Finalize(
            toposortedNodes: sorted,
            outputNodes: _outputNodes,
            inputNodes: _inputNodes,
            syntaxNodes: _syntaxNodes,
            namedNodes: _variables,
            ignorePreferred: ignorePrefered,
            namedTypeRegistry: NamedTypeRegistry,
            isRecursion: IsRecursion);
        PrintTrace("5. Finalized");

        return results;
    }

    /// <summary>
    /// Post-Push SCC closure stage. For each cyclic contractive SCC in the constraint graph,
    /// iterate Push-to-fixpoint so co-recursive μ-types in return positions fold to canonical
    /// form. Single-pass Push leaves chains like opt(ref(opt(ref(...)))) at self-referential
    /// return positions because the cycle requires multiple traversals to propagate the F-bound
    /// from other branches into the recursive return path. Per Cousot '77 / Kildall '73
    /// monotone-dataflow on a finite-height lattice (ConstraintsState narrowing), iteration
    /// converges.
    /// </summary>
    private void ScCClosurePass(TicNode[] sorted) {
        // Fast path: this stage exists only for cyclic μ-recursive types.
        // Most expressions have no recursive structure — skip in O(n) by
        // probing for any node with cyclic shape (StructBound from F-bound
        // lift, or contractive cycle head). Avoids list allocation +
        // Tarjan SCC traversal for non-recursive code.
        if (!HasAnyRecursiveCandidate(sorted))
            return;

        var roots = new List<TicNode>(sorted.Length + _variables.Count + _typeVariables.Count);
        for (int i = 0; i < sorted.Length; i++) if (sorted[i] != null) roots.Add(sorted[i]);
        foreach (var v in _variables.Values) roots.Add(v);
        foreach (var tv in _typeVariables) roots.Add(tv);

        var sccs = TarjanScc.ComputeSccs(roots);
        foreach (var scc in sccs) {
            if (!TarjanScc.IsCyclicScc(scc)) continue;
            if (!TarjanScc.IsContractive(scc)) continue;
            PushUntilFixpoint(scc, maxIterations: 10);
        }
    }

    /// <summary>O(n) scan: any node with cyclic or F-bound shape?</summary>
    private static bool HasAnyRecursiveCandidate(TicNode[] sorted) {
        for (int i = 0; i < sorted.Length; i++) {
            var n = sorted[i];
            if (n == null) continue;
            if (n.IsContractiveCycleHead) return true;
            if (n.State is ConstraintsState cs && cs.HasStructBound) return true;
            if (n.State is StateStruct s && SolvingFunctions.StructIsRecursiveCycle(s, n)) return true;
        }
        return false;
    }

    private static void PushUntilFixpoint(IReadOnlyList<TicNode> scc, int maxIterations) {
        for (int iter = 0; iter < maxIterations; iter++) {
            bool changed = false;
            foreach (var n in scc) {
                if (n == null) continue;
                if (n.IsMemberOfAnything) continue;
                var beforeState = n.State;
                var beforeDesc = (n.State as ConstraintsState)?.Descendant;
                var beforeAnc = (n.State as ConstraintsState)?.Ancestor;
                SolvingFunctions.PushConstraintsForNode(n);
                if (!ReferenceEquals(beforeState, n.State)) { changed = true; continue; }
                if (n.State is ConstraintsState cs) {
                    if (!ReferenceEquals(beforeDesc, cs.Descendant)) { changed = true; continue; }
                    if (!ReferenceEquals(beforeAnc, cs.Ancestor)) { changed = true; continue; }
                }
            }
            if (!changed) return;
        }
    }

    public TicNode[] GetNodes() => _variables.Values.Union(_syntaxNodes.Where(s => s != null)).ToArray();

    private TicNode[] Toposort(Action<TicNode> onNodeReady = null) {
        var toposortAlgorithm = new NodeToposort(
            capacity: _syntaxNodesLength + _variables.Count + _typeVariables.Count);

        for (int i = 0; i < _syntaxNodesLength; i++) toposortAlgorithm.AddToTopology(_syntaxNodes[i]);
        foreach (var node in _variables.Values) toposortAlgorithm.AddToTopology(node);
        foreach (var node in _typeVariables) toposortAlgorithm.AddToTopology(node);

        toposortAlgorithm.OptimizeTopology(onNodeReady);
        return toposortAlgorithm.NonReferenceOrdered;
    }

    #endregion

    public void PrintTrace(string name)
    {
#if DEBUG
            TraceLog.WriteLine($"\r\nTrace for {name}");
            SolvingFunctions.PrintTrace(
                _syntaxNodes
                    .Union(_variables.Select(v => v.Value))
                    .Union(_typeVariables));
#endif
    }

    private static void PrintTrace(string name, IEnumerable<TicNode> sorted)
    {
#if DEBUG
            TraceLog.WriteLine($"\r\n Sorted trace for {name}");
            SolvingFunctions.PrintTrace(sorted);
#endif
    }
}
