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

    public TypeInferenceResults Build() =>
        new TypeInferenceResults(
            bodyTypeSolving: _bodyTypeSolving,
            genericFunctionTypes: _genericFunctionTypes ?? Array.Empty<StateRefTo[]>(),
            functionalVariables: _functionalVariables ?? Array.Empty<IFunctionSignature>(),
            recursiveCalls: _recursiveCalls ?? Array.Empty<StateFun>(),
            resolvedCallArgs: _resolvedCallArgs,
            resolvedCallSignatures: _resolvedCallSignatures,
            narrowedVariables: _narrowedVariables
        );

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
