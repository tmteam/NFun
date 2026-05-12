using System;
using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing;
using NFun.Tic;
using NFun.Tic.SolvingStates;

namespace NFun.TypeInferenceAdapter;

public class TypeInferenceResultsBuilder {
    private StateRefTo[][] _genericFunctionTypes;
    private IFunctionSignature[] _functionalVariables;
    private StateFun[] _recursiveCalls;
    private readonly Dictionary<string, StateFun> _userFunctionSignatures = new();
    private Dictionary<int, ISyntaxNode[]> _resolvedCallArgs;
    private Dictionary<int, IFunctionSignature> _resolvedCallSignatures;
    private Dictionary<int, string> _narrowedVariables;

    private ITicResults _bodyTypeSolving;

    /// <param name="maxNodeId">
    /// Pre-size hint for node-indexed arrays. When > 0, arrays are allocated once
    /// at exactly this size. When 0 (user function path), arrays grow on demand.
    /// </param>
    public TypeInferenceResultsBuilder(int maxNodeId = 0) {
        if (maxNodeId > 0) {
            _genericFunctionTypes = new StateRefTo[maxNodeId][];
            _functionalVariables = new IFunctionSignature[maxNodeId];
            _recursiveCalls = new StateFun[maxNodeId];
        }
    }

    public void RememberGenericCallArguments(int id, StateRefTo[] types) {
        EnsureCapacity(ref _genericFunctionTypes, id);
        _genericFunctionTypes[id] = types;
    }

    public StateFun GetUserFunctionSignature(string id, int argsCount) {
        if (_userFunctionSignatures.Count == 0)
            return null;
        string name = id + "'" + argsCount;
        _userFunctionSignatures.TryGetValue(name, out var res);
        return res;
    }

    public void RememberUserFunctionSignature(string name, StateFun signature)
        => _userFunctionSignatures.Add(name + "'" + signature.ArgsCount, signature);

    public void RememberFunctionalVariable(int id, IFunctionSignature signature) {
        EnsureCapacity(ref _functionalVariables, id);
        _functionalVariables[id] = signature;
    }

    public void RememberResolvedCallArgs(int orderNumber, ISyntaxNode[] args)
        => (_resolvedCallArgs ??= new())[orderNumber] = args;

    public void RememberResolvedCallSignature(int orderNumber, IFunctionSignature signature)
        => (_resolvedCallSignatures ??= new())[orderNumber] = signature;

    public void SetResults(ITicResults bodyTypeSolving) => _bodyTypeSolving = bodyTypeSolving;

    public TypeInferenceResults Build() {
        // Fill genericFunctionTypes[i] for recursive call sites with the function body's own generics.
        // Per Amadio–Cardelli equirecursion, a recursive self-call's generic vector equals the
        // enclosing instance's. Otherwise body-internal generics (operator T from ==/+, F-bounded CSs
        // surfaced through cycle handling) would be left as default FunnyType (Empty) by
        // CalcGenericArgTypeList(recSignature), breaking downstream substitution.
        if (_bodyTypeSolving != null && _recursiveCalls != null)
        {
            var bodyGenerics = _bodyTypeSolving.GenericsStates;
            if (bodyGenerics.Count > 0)
            {
                StateRefTo[] sharedRefs = null;
                for (int i = 0; i < _recursiveCalls.Length; i++)
                {
                    if (_recursiveCalls[i] == null) continue;
                    // Lazily build a single shared StateRefTo[] — every recursive
                    // call site of THIS function shares the same generic vector
                    // (Amadio–Cardelli identity). All call sites point at the
                    // SAME ITicNodeState instances (the function body's CSs)
                    // so GenericMapConverter's IndexOf(_constrainsMap, cs)
                    // finds them by reference.
                    sharedRefs ??= BuildSharedGenericRefs(bodyGenerics);
                    EnsureCapacity(ref _genericFunctionTypes, i);
                    if (_genericFunctionTypes[i] == null)
                        _genericFunctionTypes[i] = sharedRefs;
                }
            }
        }
        return new TypeInferenceResults(
            bodyTypeSolving: _bodyTypeSolving,
            genericFunctionTypes: _genericFunctionTypes ?? Array.Empty<StateRefTo[]>(),
            functionalVariables: _functionalVariables ?? Array.Empty<IFunctionSignature>(),
            recursiveCalls: _recursiveCalls ?? Array.Empty<StateFun>(),
            resolvedCallArgs: _resolvedCallArgs,
            resolvedCallSignatures: _resolvedCallSignatures,
            narrowedVariables: _narrowedVariables
        );
    }

    /// <summary>
    /// Wrap each body-generic ConstraintsState in a fresh StateRefTo whose
    /// target TicNode carries that exact CS instance. ExpressionBuilder's
    /// converter at the recursive call site then sees StateRefTo→Element ==
    /// the CS instance, finds it by reference in _constrainsMap, and returns
    /// the outer call's _argTypes[i]. Net effect: recursive call's
    /// concreteTypes[i] = outer concreteTypes[i] for every body-internal
    /// generic.
    /// </summary>
    private static StateRefTo[] BuildSharedGenericRefs(IReadOnlyList<ConstraintsState> bodyGenerics) {
        var refs = new StateRefTo[bodyGenerics.Count];
        for (int j = 0; j < bodyGenerics.Count; j++)
        {
            var node = TicNode.CreateInvisibleNode(bodyGenerics[j]);
            refs[j] = new StateRefTo(node);
        }
        return refs;
    }

    public void RememberRecursiveCall(int id, StateFun userFunction) {
        EnsureCapacity(ref _recursiveCalls, id);
        _recursiveCalls[id] = userFunction;
    }

    public void RememberNarrowedVariable(int orderNumber, string originalVariableName)
        => (_narrowedVariables ??= new())[orderNumber] = originalVariableName;

    /// <summary>
    /// Ensures the array can hold index <paramref name="index"/>.
    /// When the array is null (no pre-size hint), allocates with a reasonable initial size.
    /// When the array is too small, doubles its capacity.
    /// </summary>
    private static void EnsureCapacity<T>(ref T[] array, int index) {
        if (array == null) {
            array = new T[Math.Max(index + 1, 16)];
            return;
        }
        if (index < array.Length) return;
        var newSize = Math.Max(array.Length * 2, index + 1);
        Array.Resize(ref array, newSize);
    }
}

public class TypeInferenceResults {
    private readonly ITicResults _bodyTypeSolving;
    private readonly IFunctionSignature[] _functionalVariables;
    private readonly StateFun[] _recursiveCalls;
    private readonly Dictionary<int, ISyntaxNode[]> _resolvedCallArgs;
    private readonly Dictionary<int, IFunctionSignature> _resolvedCallSignatures;
    private readonly Dictionary<int, string> _narrowedVariables;

    public TypeInferenceResults(
        ITicResults bodyTypeSolving,
        StateRefTo[][] genericFunctionTypes,
        IFunctionSignature[] functionalVariables,
        StateFun[] recursiveCalls,
        Dictionary<int, ISyntaxNode[]> resolvedCallArgs = null,
        Dictionary<int, IFunctionSignature> resolvedCallSignatures = null,
        Dictionary<int, string> narrowedVariables = null) {
        GenericFunctionTypes = genericFunctionTypes;
        _bodyTypeSolving = bodyTypeSolving;
        _functionalVariables = functionalVariables;
        _recursiveCalls = recursiveCalls;
        _resolvedCallArgs = resolvedCallArgs;
        _resolvedCallSignatures = resolvedCallSignatures;
        _narrowedVariables = narrowedVariables;
    }

    public IFunctionSignature GetFunctionalVariableOrNull(int id) =>
        _functionalVariables.Length <= id
            ? null
            : _functionalVariables[id];

    public ITicNodeState[] GetGenericCallArguments(int id) =>
        GenericFunctionTypes.Length <= id
            ? null
            : GenericFunctionTypes[id];

    public StateFun GetRecursiveCallOrNull(int id) =>
        _recursiveCalls.Length <= id
            ? null
            : _recursiveCalls[id];

    public  ITicNodeState[][] GenericFunctionTypes { get; }
    public IReadOnlyList<ConstraintsState> Generics => _bodyTypeSolving.GenericsStates;

    public ISyntaxNode[] GetResolvedCallArgsOrNull(int id) =>
        _resolvedCallArgs != null && _resolvedCallArgs.TryGetValue(id, out var args) ? args : null;

    public IFunctionSignature GetResolvedCallSignatureOrNull(int id) =>
        _resolvedCallSignatures != null && _resolvedCallSignatures.TryGetValue(id, out var sig) ? sig : null;

    public ITicNodeState GetSyntaxNodeTypeOrNull(int id)
        => _bodyTypeSolving.GetSyntaxNodeOrNull(id)?.State;
    public ITicNodeState GetVariableTypeOrNull(string name)
        => _bodyTypeSolving.GetVariableNodeOrNull(name)?.State;
    public ITicNodeState GetVariableType(string name)
        => _bodyTypeSolving.GetVariableNode(name).State;

    public string GetNarrowedVariableOrNull(int orderNumber) =>
        _narrowedVariables != null && _narrowedVariables.TryGetValue(orderNumber, out var name) ? name : null;
}
