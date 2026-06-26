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

    internal Types.INamedTypeFieldRegistry _namedTypeRegistry;
    internal Types.INamedTypeFieldRegistry NamedTypeRegistry {
        get => _namedTypeRegistry;
        set {
            _namedTypeRegistry = value;
            // Named-type registry is the only entry for declared μ-recursive shapes;
            // flips IsRecursion so cycle-aware passes skip when there's nothing to do.
            if (value != null) IsRecursion = true;
        }
    }

    /// <summary>
    /// True iff the graph may produce μ-recursive types (named-type registry, `?.`, or
    /// user-function body). Gates the visited-pair guard in StagesExtension and all
    /// cycle-aware destruction passes — false ⇒ zero cycle-detection cost.
    /// </summary>
    public bool IsRecursion { get; set; }

    public GraphBuilder() { _syntaxNodes = new TicNode[16]; _syntaxNodesLength = 16; }
    public GraphBuilder(int maxSyntaxNodeId) { _syntaxNodes = new TicNode[maxSyntaxNodeId]; _syntaxNodesLength = maxSyntaxNodeId; }

    public StateRefTo InitializeVarNode(ITypeState desc = null, StatePrimitive anc = null, bool isComparable = false)
        => new(CreateVarType(ConstraintsState.Of(desc, anc, isComparable)));

    /// <summary>
    /// Generic var node with composite-shape constraint (Enumerable&lt;T&gt; etc.).
    /// See Specs/Tic/Algebra_CompositeConstraints.md §4.1.1.
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

    /// <summary>Lookup-only variant of <see cref="GetNamedNode"/> — null if absent, no side effects.</summary>
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

    /// <summary>SetCallArgument fast path: convert subsumed-range fresh CS arg to direct ref, skip ancestor edge.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCallArgument(StateRefTo type, int argId) {
        var node = GetOrCreateNode(argId);
        if (node.Ancestors.Count == 0 && TryConvertConstToRef(node, type))
            return;
        node.AddAncestor(type.Node);
    }

    /// <summary>Const-ref conversion: fresh CS arg whose range fits inside the generic's range becomes a direct ref.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TryConvertConstToRef(TicNode node, StateRefTo type) {
        if (node.IsMemberOfAnything)
            return false;
        if (node.State is not ConstraintsState argCs || !argCs.HasDescendant)
            return false;
        if (type.Node.State is not ConstraintsState genCs)
            return false;
        if (genCs.Descendant is not StatePrimitive genDesc) return false;
        if (argCs.Descendant is not StatePrimitive argDesc) return false;
        if (genCs.Ancestor == null || argCs.Ancestor == null) return false;
        if (!argDesc.CanBePessimisticConvertedTo(genDesc)) return false;
        if (!genCs.Ancestor.CanBePessimisticConvertedTo(argCs.Ancestor)) return false;
        if (argCs.IsComparable && !genCs.IsComparable) return false;
        // Preferred carries provenance — must survive the CS→Ref conversion.
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
                // Composite-shape signature param (Enumerable<T>): register ElementNode alongside.
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
                // Signature param's composite shape is rigid: Opt(T) ≤ T invalid (lift would change contract).
                ancestor.IsSignatureParam = true;
                node.AddAncestor(ancestor);
                break;
            }
            case StateRefTo refTo:
            {
                // Guard self-loop: arg already refs target → AddAncestor would self-cycle through toposort ref-transfer.
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
                // Composite re-annotation on a solved node would trip the state-setter assertion;
                // surface as a clean FU879 via the caller's TrySetVarType=false branch.
                if (!node.IsMutable && !state.Equals(node.State))
                    return false;
                RegistrateCompositeType(composite);
                node.State = state;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Like <see cref="SetDef"/> but the variable is NOT registered as output. For parameter defaults.</summary>
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

        // Identity-share rule literal (StateFun) into an unconstrained var so SetCall later finds
        // StateFun directly and preserves IsSignatureParam rigidity (without it, the SetCall ELSE
        // synthesizes fresh args that widen at call sites). Skip the Pull edge — Apply(StateFun,
        // StateFun) on identity-shared state would call retNode.AddAncestor(retNode).
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

    /// <summary>Function call where the function lives at bodyId (variable or expression).</summary>
    public void SetCall(int bodyId, params int[] argThenReturnIds)
        => SetCall(GetOrCreateNode(bodyId), argThenReturnIds);

    /// <summary>Function call where the function is a named variable.</summary>
    public void SetCall(string name, params int[] argThenReturnIds)
        => SetCall(GetNamedNode(name), argThenReturnIds);

    /// <summary>Function call with a known <see cref="StateFun"/> signature.</summary>
    public void SetCall(StateFun funState, params int[] argThenReturnIds) {
        if (funState.ArgsCount != argThenReturnIds.Length - 1)
            throw new ArgumentException("Sizes of type and id array have to be equal");

        RegistrateCompositeType(funState);


        // Damas-Milner '82 let-polymorphism: F-bounded signatures (CS{StructBound}) must
        // instantiate fresh per call site or distinct callers collide on the Descendant slot.
        bool needsPerSiteClone = IsRecursion && SignatureHasRecursiveShape(funState);
        var argNodeMap = needsPerSiteClone
            ? CreatePerSiteCloneMap(funState)
            : null;

        for (int i = 0; i < funState.ArgsCount; i++)
        {
            var argNode = funState.ArgNodes[i];
            // Composite signature params are shape-rigid: Opt(T) ≤ T would change the contract.
            if (argNode.State is ICompositeState)
                argNode.IsSignatureParam = true;

            var argKey = argNode.GetNonReference();
            var effectiveArgNode = argNodeMap != null && argNodeMap.TryGetValue(argKey, out var cloned)
                ? cloned
                : argNode;
            var state = effectiveArgNode.State;
            // Damas-Milner '82 §3 let-monomorphic body: each param-use is a reflexive constraint,
            // so the call adds an edge. Route through RefTo for CS, StateFun (members share even
            // when solved → self-call would self-loop), and unsolved composites.
            if (state is ConstraintsState
                || state is StateFun
                || (state is ICompositeState comp && !comp.IsSolved))
                state = new StateRefTo(effectiveArgNode);
            SetCallArgument(state, argThenReturnIds[i]);
        }

        var returnId = argThenReturnIds[^1];
        var returnNode = GetOrCreateNode(returnId);
        // Contractive-cycle returns (F-bound) must NOT MergeInplace: that would either share
        // cycle TicNodes across solves or hit Circular ancestor. Route via RefTo so the cycle
        // stays internal to the signature.
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

    /// <summary>True iff the signature carries a μ-recursive cycle (F-bound) and needs per-call cloning.</summary>
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
    /// Per-call-site clone of the signature subgraph (Damas-Milner '82 instantiation).
    /// Placeholder pattern closes μ-cycles: new node registered before recursing into state.
    /// </summary>
    private Dictionary<TicNode, TicNode> CreatePerSiteCloneMap(StateFun funState) {
        var map = new Dictionary<TicNode, TicNode>();
        foreach (var arg in funState.ArgNodes)
            DeepCloneNode(arg, map);
        // RetNode not pre-cloned: SetCall handles return separately, and cloning here would
        // break arg/return sharing for monovariant μ.
        return map.Count > 0 ? map : null;
    }

    private TicNode DeepCloneNode(TicNode original, Dictionary<TicNode, TicNode> map) {
        var nr = original.GetNonReference();
        if (map.TryGetValue(nr, out var existing)) return existing;
        // Monomorphic — share by reference.
        if (nr.State is StatePrimitive) return nr;
        // Solved StateFun: ArgNodes/RetNode are concrete; sharing is mandatory because a clone
        // would alias the originals' inner nodes and Apply(StateFun, StateFun) would self-loop.
        if (nr.State is StateFun fn && fn.IsSolved) return nr;

        // Placeholder: register before recursing so back-edges close the cycle in the clone.
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
                // Arrays preserve element sharing — element is typically primitive, no cycle.
                return StateArray.Of(arr.ElementNode);
            case StateStruct s when s.TypeName != null:
                // Named types: identity comes from the registry; sharing is safe.
                return s;
            case StateStruct s:
                // Anonymous (potentially recursive): clone fields through the map to close cycles.
                var newFields = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
                foreach (var (name, fn) in s.Fields)
                    newFields[name] = DeepCloneNode(fn, map);
                return new StateStruct(newFields, isFrozen: s.IsFrozen, isOpen: s.IsOpen) {
                    TypeName = s.TypeName,
                    IsOptionalSourced = s.IsOptionalSourced,
                };
            case StateFun funState:
                // Unsolved: clone inner nodes through the map (solved StateFun short-circuited above).
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
    /// True iff n reaches itself through composite-member edges (μ-cycle), or is a marked head.
    /// Visited is back-tracked so a shared DAG node isn't misreported as a cycle.
    /// At graph-build time NodeToposort hasn't run, so the flag alone is insufficient.
    /// </summary>
    private static bool ContainsContractiveCycle(TicNode n, HashSet<TicNode> visited) {
        var nr = n.GetNonReference();
        if (nr.IsContractiveCycleHead) return true;
        if (nr.State is not ICompositeState composite) return false;
        if (!visited.Add(nr)) return true; // back-edge through composites
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

    /// <summary>Binary pure-generic call (T,T):T.</summary>
    public void SetCall(StateRefTo generic, int arg0Id, int arg1Id, int returnId) {
        SetCallArgument(generic, arg0Id);
        SetCallArgument(generic, arg1Id);
        MergeOrSetNode(returnId, generic);
    }

    /// <summary>Unary pure-generic call (T):T.</summary>
    public void SetCall(StateRefTo generic, int argId, int returnId) {
        SetCallArgument(generic, argId);
        MergeOrSetNode(returnId, generic);
    }

    /// <summary>Pure-generic call (T,T...):T.</summary>
    public void SetCall(StateRefTo generic, int[] argThenReturnIds) {
        for (int i = 0; i < argThenReturnIds.Length - 1; i++)
            SetCallArgument(generic, argThenReturnIds[i]);

        var returnId = argThenReturnIds[^1];
        MergeOrSetNode(returnId, generic);
    }

    /// <summary>Function call with an explicit per-argument signature.</summary>
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

        // When functionNode is RefTo(target), the synthesized StateFun goes on target.State
        // — overwriting functionNode.State would sever the link and orphan the declared field type.
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
        // Broadcast to thread-static: Stages.Invoke's visited-pair guard short-circuits when false.
        Stages.StagesExtension._isRecursion = IsRecursion;

        // None presence dictates Pull strategy: absent ⇒ fused Toposort+Pull (single pass);
        // present ⇒ two-phase Pull (IsOptional ordering, see specs_tic/TicAlgorithm.md).
        bool hasNone = false;
        for (int i = 0; i < _syntaxNodesLength; i++)
            if (_syntaxNodes[i]?.State == StatePrimitive.None) { hasNone = true; break; }

        bool hasOptionalTypes;
        TicNode[] sorted;
        if (!hasNone) {
            sorted = Toposort(node => {
                if (!node.IsMemberOfAnything)
                    SolvingFunctions.PullConstraintsForNode(node);
            });
            hasOptionalTypes = false;
            PrintTrace("1+2. Toposorted+Pulled (fused)", sorted);
        } else {
            sorted = Toposort();
            PrintTrace("1. Toposorted", sorted);
            SolvingFunctions.PullConstraintsTwoPhase(sorted);
            hasOptionalTypes = true;
            PrintTrace("2. PullConstraints (two-phase)", sorted);
        }

        // Propagate Preferred BEFORE Push: Push collapses literal CS to bare primitive when its
        // ancestor pins it, dropping Preferred. Preferred is metadata (TicPreferred.md P3
        // monotonicity), so propagating between Pull and Push is sound.
        SolvingFunctions.PropagatePreferred(sorted);
        SolvingFunctions.PushConstraints(sorted);
        // SCC closure for contractive cycles (Amadio-Cardelli §4.2 / Pottier-Rémy '05 §10.6):
        // single-pass Push leaves degenerate ref-chains in co-recursive returns; iterate to
        // fixpoint per cyclic SCC to produce the canonical regular tree.
        if (IsRecursion) ScCClosurePass(sorted);
        PrintTrace("3. PushConstraints", sorted);

        // Thread NamedTypeRegistry explicitly (no globals) so concurrent solves stay isolated.
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
    /// Post-Push: iterate Push to fixpoint on each cyclic contractive SCC so co-recursive
    /// μ-types fold to canonical form. Converges by Cousot '77 / Kildall '73 monotone-dataflow
    /// on the finite-height CS-narrowing lattice.
    /// </summary>
    private void ScCClosurePass(TicNode[] sorted) {
        // O(n) probe avoids list allocation + Tarjan SCC traversal when no cycle exists.
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
