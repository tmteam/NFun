﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;

namespace NFun.Tic;

public class GraphBuilder {
    private readonly Dictionary<string, TicNode> _variables = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<TicNode> _syntaxNodes;
    private readonly List<TicNode> _typeVariables = new();
    private int _varNodeId = 0;
    private readonly List<TicNode> _outputNodes = new();
    private readonly List<TicNode> _inputNodes = new();

    public StateRefTo InitializeVarNode(ITypeState desc = null, StatePrimitive anc = null, bool isComparable = false)
        => new(CreateVarType(new ConstrainsState(desc, anc, isComparable)));

    public GraphBuilder() => _syntaxNodes = new List<TicNode>(16);
    public GraphBuilder(int maxSyntaxNodeId) => _syntaxNodes = new List<TicNode>(maxSyntaxNodeId);


    #region set primitives

    public void SetVar(string name, int node) {
        var namedNode = GetNamedNode(name);
        var idNode = GetOrCreateNode(node);
        if (idNode.State is ConstrainsState)
        {
            namedNode.AddAncestor(idNode);
        }
        else
        {
            throw new InvalidOperationException(
                $"Node {node} cannot be referenced by '{name}' because it is not constrained node.");
        }
    }

    public void SetIfElse(int[] conditions, int[] expressions, int resultId) {
        var result = GetOrCreateNode(resultId);

        foreach (var exprId in expressions)
        {
            var expr = GetOrCreateNode(exprId);
            expr.AddAncestor(result);
        }

        foreach (var condId in conditions)
            SetOrCreatePrimitive(condId, StatePrimitive.Bool);
    }

    public void SetConst(int id, StatePrimitive type)
        => SetOrCreatePrimitive(id, type);

    public void SetIntConst(int id, StatePrimitive desc)
        => SetGenericConst(id,
            desc: desc,
            anc: StatePrimitive.Real,
            preferred: StatePrimitive.Real);

    public void SetGenericConst(int id, StatePrimitive desc = null, StatePrimitive anc = null, StatePrimitive preferred = null) {
        var node = GetOrCreateNode(id);
        if (node.State is ConstrainsState constrains)
        {
            constrains.AddAncestor(anc);
            constrains.AddDescendant(desc);
            constrains.Preferred = preferred;
        }
        else
            throw new InvalidOperationException();
    }

    public bool HasNamedNode(string s) => _variables.ContainsKey(s);

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

    public void SetArrayConst(int id, StatePrimitive elementType) {
        var eNode = CreateVarType(elementType);
        var node = GetOrCreateNode(id);
        if (node.State is ConstrainsState c)
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

    public void SetStructConst(int id, StateStruct @struct) {
        if (!@struct.IsSolved)
            throw new InvalidOperationException();
        GetOrCreateStructNode(id, @struct);
        RegistrateCompositeType(@struct);
    }

    public void CreateLambda(int returnId, int lambdaId, params string[] varNames) {
        var args = GetNamedNodes(varNames);
        var ret = GetOrCreateNode(returnId);
        SetOrCreateLambda(lambdaId, args, ret);
    }

    public void CreateLambda(int returnId, int lambdaId, ITypeState returnType, params string[] varNames) {
        var args = GetNamedNodes(varNames);
        var exprId = GetOrCreateNode(returnId);
        var returnTypeNode = CreateVarType(returnType);
        exprId.AddAncestor(returnTypeNode);
        //expr<=returnType<= ...
        SetOrCreateLambda(lambdaId, args, returnTypeNode);
    }

    public StateFun SetFunDef(string name, int returnId, ITypeState returnType = null, params string[] varNames) {
        var args = GetNamedNodes(varNames);
        var exprId = GetOrCreateNode(returnId);
        var returnTypeNode = CreateVarType(returnType);
        //expr<=returnType<= ...
        exprId.AddAncestor(returnTypeNode);
        var fun = StateFun.Of(args, returnTypeNode);

        var node = GetNamedNode(name);
        if (node.State is not ConstrainsState c || !c.NoConstrains)
            throw new InvalidOperationException($"variable {name}already declared");
        node.State = fun;
        _outputNodes.Add(returnTypeNode);
        _inputNodes.AddRange(args);
        return fun;
    }

    public StateRefTo SetStrictArrayInit(int resultIds, params int[] elementIds) {
        var elementType = CreateVarType();
        GetOrCreateArrayNode(resultIds, elementType);

        foreach (var id in elementIds)
        {
            elementType.BecomeReferenceFor(GetOrCreateNode(id));
            elementType.IsMemberOfAnything = true;
        }

        return new StateRefTo(elementType);
    }

    public void SetSoftArrayInit(int resultIds, params int[] elementIds) {
        var elementType = CreateVarType();
        GetOrCreateArrayNode(resultIds, elementType);
        foreach (var id in elementIds)
        {
            GetOrCreateNode(id).AddAncestor(elementType);
            elementType.IsMemberOfAnything = true;
        }
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
            if (state is ConstrainsState)
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

    public void SetDef(string name, int rightNodeId) {
        var exprNode = GetOrCreateNode(rightNodeId);
        var defNode = GetNamedNode(name);
        _outputNodes.Add(defNode);

        if (exprNode.State is StatePrimitive primitive && defNode.State is ConstrainsState constrains)
            constrains.Preferred = primitive;

        exprNode.AddAncestor(defNode);
    }

    public void SetFieldAccess(int structNodeId, int opId, string fieldName) {
        var node = GetOrCreateStructNode(structNodeId, new StateStruct())
            .GetNonReference();

        var state = (StateStruct)node.State;
        var memberNode = state.GetFieldOrNull(fieldName);
        if (memberNode == null)
        {
            memberNode = CreateVarType();
            //if origin node is reference node - than we should change
            node.State = state.With(fieldName, memberNode);
        }

        MergeOrSetNode(opId, new StateRefTo(memberNode));
    }

    public void SetStructInit(string[] fieldNames, int[] fieldExpressionIds, int id) {
        var fields = new Dictionary<string, TicNode>(fieldNames.Length);
        for (int i = 0; i < fieldNames.Length; i++)
        {
            fields.Add(fieldNames[i], GetOrCreateNode(fieldExpressionIds[i]));
        }

        GetOrCreateStructNode(id, new StateStruct(fields, false));
    }

    public void SetCompareChain(int nodeOrderNumber, StateRefTo[] generics, int[] ids) {
        for (int i = 0; i < generics.Length; i++)
        {
            var generic = generics[i];
            SetCallArgument(generic, ids[i]);
            SetCallArgument(generic, ids[i + 1]);
        }

        SetOrCreatePrimitive(nodeOrderNumber, StatePrimitive.Bool);
    }

    #endregion


    public ITicResults Solve(bool ignorePrefered = false) {
        PrintTrace("0. Solving");

        var sorted = Toposort();
        PrintTrace("1. Toposorted");
        PrintTrace("1. Toposorted", sorted);

        SolvingFunctions.PullConstraints(sorted);
        PrintTrace("2. PullConstraints");
        PrintTrace("2. PullConstraints", sorted);

        SolvingFunctions.PushConstraints(sorted);
        PrintTrace("3. PushConstraints");
        PrintTrace("3. PushConstraints", sorted);

        bool allTypesAreSolved = SolvingFunctions.Destruction(sorted);
        PrintTrace("4. Destructed");

        if (allTypesAreSolved)
            return new TicResultsWithoutGenerics(_variables, _syntaxNodes);

        var results =  SolvingFunctions.Finalize(
            toposortedNodes: sorted,
            outputNodes: _outputNodes,
            inputNodes: _inputNodes,
            syntaxNodes: _syntaxNodes,
            namedNodes: _variables,
            ignorePrefered);
        PrintTrace("4. Finalized");

        return results;
    }
    public TicNode[] GetNodes() => _variables.Values.Union(_syntaxNodes.Where(s=>s!=null)).ToArray();
    private TicNode[] Toposort() {
        var toposortAlgorithm = new NodeToposort(
            capacity: _syntaxNodes.Count + _variables.Count + _typeVariables.Count);

        foreach (var node in _syntaxNodes) toposortAlgorithm.AddToTopology(node);
        foreach (var node in _variables.Values) toposortAlgorithm.AddToTopology(node);
        foreach (var node in _typeVariables) toposortAlgorithm.AddToTopology(node);

        toposortAlgorithm.OptimizeTopology();
        return toposortAlgorithm.NonReferenceOrdered;
    }

    /// <summary>
    /// Optimized version of SetCallArgument for ref cases
    /// Not sure how necessary this optimization is
    /// </summary>
    private void SetCallArgument(StateRefTo type, int argId) {
        var node = GetOrCreateNode(argId);
        node.AddAncestor(type.Node);
    }

    private void SetCallArgument(ITicNodeState type, int argId) {
        var node = GetOrCreateNode(argId);
        switch (type)
        {
            case StatePrimitive primitive:
            {
                if (node.State is ConstrainsState { HasAncestor: false, HasDescendant: false })
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
            if (state is not ConstrainsState constrains || !constrains.CanBeConvertedTo(newFunVar))
                throw TicErrors.IsNotAFunctionalVariableOrFunction(functionNode, newFunVar);
            functionNode.State = newFunVar;
            SetCall(newFunVar, argThenReturnIds);
        }
    }

    private TicNode[] GetNamedNodes(string[] names) {
        var ans = new TicNode[names.Length];
        for (int i = 0; i < names.Length; i++)
            ans[i] = GetNamedNode(names[i]);

        return ans;
    }

    private TicNode GetNamedNode(string name) {
        if (_variables.TryGetValue(name, out var varnode))
        {
            return varnode;
        }

        var ans = TicNode.CreateNamedNode(name, new ConstrainsState());
        _variables.Add(name, ans);
        return ans;
    }

    private void SetOrCreateLambda(int lambdaId, TicNode[] args, TicNode ret) {
        var fun = StateFun.Of(args, ret);

        var alreadyExists = _syntaxNodes.GetOrEnlarge(lambdaId);
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

    private void SetOrCreatePrimitive(int id, StatePrimitive type) {
        var node = GetOrCreateNode(id);
        if (!node.TryBecomeConcrete(type))
            throw TicErrors.CannotSetState(node, type);
    }

    private void GetOrCreateArrayNode(int id, TicNode elementType) {
        var alreadyExists = _syntaxNodes.GetOrEnlarge(id);
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

    private TicNode GetOrCreateStructNode(int id, StateStruct stateStruct) {
        var alreadyExists = _syntaxNodes.GetOrEnlarge(id);
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

    /// <summary>
    /// Returns already exists syntax node id, or creates new one with empty constraints
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TicNode GetOrCreateNode(int id) {
        var alreadyExists = _syntaxNodes.GetOrEnlarge(id);
        if (alreadyExists != null)
            return alreadyExists;

        var res = TicNode.CreateSyntaxNode(id, new ConstrainsState(), true);
        _syntaxNodes[id] = res;
        return res;
    }

    /// <summary>
    /// Merge already exists syntax node id,
    /// or creates new one with specified type
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MergeOrSetNode(int id, StateRefTo type) {
        var alreadyExists = _syntaxNodes.GetOrEnlarge(id);
        if (alreadyExists == null)
        {
            var res = TicNode.CreateSyntaxNode(id, type, true);
            _syntaxNodes[id] = res;
            return;
        }

        alreadyExists.State = SolvingFunctions.GetMergedStateOrNull(alreadyExists.State, type) ??
                              throw TicErrors.CannotSetState(alreadyExists, type);
    }

    private void RegistrateCompositeType(ICompositeState composite) {
        foreach (var member in composite.Members)
        {
            if (!member.Registered)
            {
                member.Registered = true;
                if (member.State is ICompositeState c)
                    RegistrateCompositeType(c);
                _typeVariables.Add(member);
            }
        }
    }

    private TicNode CreateVarType(ITicNodeState state = null) {
        if (state is ICompositeState composite)
            RegistrateCompositeType(composite);

        var varNode = TicNode.CreateTypeVariableNode(
            name: "V" + _varNodeId,
            state: state ?? new ConstrainsState(),
            true);
        _varNodeId++;
        _typeVariables.Add(varNode);
        return varNode;
    }

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
