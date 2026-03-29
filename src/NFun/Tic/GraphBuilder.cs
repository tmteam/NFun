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

    public GraphBuilder() { _syntaxNodes = new TicNode[16]; _syntaxNodesLength = 16; }
    public GraphBuilder(int maxSyntaxNodeId) { _syntaxNodes = new TicNode[maxSyntaxNodeId]; _syntaxNodesLength = maxSyntaxNodeId; }

    public StateRefTo InitializeVarNode(ITypeState desc = null, StatePrimitive anc = null, bool isComparable = false)
        => new(CreateVarType(ConstraintsState.Of(desc, anc, isComparable)));

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
            case ICompositeState composite:
            {
                RegistrateCompositeType(composite);
                var ancestor = CreateVarType(composite);
                node.AddAncestor(ancestor);
                break;
            }
            case StateRefTo refTo:
            {
                node.AddAncestor(refTo.Node);
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
        _outputNodes.Add(defNode);

        if (exprNode.State is StatePrimitive primitive && defNode.State is ConstraintsState constrains)
            constrains.Preferred = primitive;

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

        for (int i = 0; i < funState.ArgsCount; i++)
        {
            var state = funState.ArgNodes[i].State;
            if (state is ConstraintsState)
                state = new StateRefTo(funState.ArgNodes[i]);
            SetCallArgument(state, argThenReturnIds[i]);
        }

        var returnId = argThenReturnIds[^1];
        var returnNode = GetOrCreateNode(returnId);
        SolvingFunctions.MergeInplace(funState.RetNode, returnNode);
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

        var state = functionNode.State;
        if (state is StateRefTo r)
            state = r.Node.State;

        if (state is StateFun fun)
        {
            if (fun.ArgsCount != argThenReturnIds.Length - 1)
                throw TicErrors.InvalidFunctionalVariableSignature(functionNode);

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
                throw TicErrors.IsNotAFunctionalVariableOrFunction(functionNode, newFunVar);
            functionNode.State = newFunVar;
            SetCall(newFunVar, argThenReturnIds);
        }
    }

    #endregion

    #region solving

    public ITicResults Solve(bool ignorePrefered = false) {
        var sorted = Toposort();
        PrintTrace("1. Toposorted", sorted);

        SolvingFunctions.PullConstraints(sorted);
        PrintTrace("2. PullConstraints", sorted);

        SolvingFunctions.PushConstraints(sorted);
        PrintTrace("3. PushConstraints", sorted);

        bool allTypesAreSolved = SolvingFunctions.Destruction(sorted);
        PrintTrace("4. Destructed");

        if (allTypesAreSolved)
            return new TicResultsWithoutGenerics(_variables, _syntaxNodes);

        var results = SolvingFunctions.Finalize(
            toposortedNodes: sorted,
            outputNodes: _outputNodes,
            inputNodes: _inputNodes,
            syntaxNodes: _syntaxNodes,
            namedNodes: _variables,
            ignorePrefered);
        PrintTrace("5. Finalized");

        return results;
    }

    public TicNode[] GetNodes() => _variables.Values.Union(_syntaxNodes.Where(s => s != null)).ToArray();

    private TicNode[] Toposort() {
        var toposortAlgorithm = new NodeToposort(
            capacity: _syntaxNodesLength + _variables.Count + _typeVariables.Count);

        for (int i = 0; i < _syntaxNodesLength; i++) toposortAlgorithm.AddToTopology(_syntaxNodes[i]);
        foreach (var node in _variables.Values) toposortAlgorithm.AddToTopology(node);
        foreach (var node in _typeVariables) toposortAlgorithm.AddToTopology(node);

        toposortAlgorithm.OptimizeTopology();
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
