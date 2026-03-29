using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing;
using NFun.Tic;
using NFun.Tic.SolvingStates;

namespace NFun.TypeInferenceAdapter; 

public class TypeInferenceResultsBuilder {
    private readonly List<StateRefTo[]> _genericFunctionTypes = new();
    private readonly List<IFunctionSignature> _functionalVariable = new();
    private readonly List<StateFun> _recursiveCalls = new();
    private readonly Dictionary<string, StateFun> _userFunctionSignatures = new();
    private Dictionary<int, ISyntaxNode[]> _resolvedCallArgs;
    private Dictionary<int, IFunctionSignature> _resolvedCallSignatures;

    private ITicResults _bodyTypeSolving;

    public void RememberGenericCallArguments(int id, StateRefTo[] types)
        => _genericFunctionTypes.EnlargeAndSet(id, types);

    public StateFun GetUserFunctionSignature(string id, int argsCount) {
        if (_userFunctionSignatures.Count == 0)
            return null;
        string name = id + "'" + argsCount;
        _userFunctionSignatures.TryGetValue(name, out var res);
        return res;
    }

    public void RememberUserFunctionSignature(string name, StateFun signature)
        => _userFunctionSignatures.Add(name + "'" + signature.ArgsCount, signature);

    public void RememberFunctionalVariable(int id, IFunctionSignature signature)
        => _functionalVariable.EnlargeAndSet(id, signature);

    public void RememberResolvedCallArgs(int orderNumber, ISyntaxNode[] args)
        => (_resolvedCallArgs ??= new())[orderNumber] = args;

    public void RememberResolvedCallSignature(int orderNumber, IFunctionSignature signature)
        => (_resolvedCallSignatures ??= new())[orderNumber] = signature;

    public void SetResults(ITicResults bodyTypeSolving) => _bodyTypeSolving = bodyTypeSolving;

    public TypeInferenceResults Build() =>
        new TypeInferenceResults(
            bodyTypeSolving: _bodyTypeSolving,
            genericFunctionTypes: _genericFunctionTypes.ToArray(),
            functionalVariables: _functionalVariable,
            recursiveCalls: _recursiveCalls,
            resolvedCallArgs: _resolvedCallArgs,
            resolvedCallSignatures: _resolvedCallSignatures
        );

    public void RememberRecursiveCall(int id, StateFun userFunction)
        => _recursiveCalls.EnlargeAndSet(id, userFunction);
}

public class TypeInferenceResults {
    private readonly ITicResults _bodyTypeSolving;
    private readonly IList<IFunctionSignature> _functionalVariables;
    private readonly IList<StateFun> _recursiveCalls;
    private readonly Dictionary<int, ISyntaxNode[]> _resolvedCallArgs;
    private readonly Dictionary<int, IFunctionSignature> _resolvedCallSignatures;

    public TypeInferenceResults(
        ITicResults bodyTypeSolving,
        StateRefTo[][] genericFunctionTypes,
        IList<IFunctionSignature> functionalVariables,
        IList<StateFun> recursiveCalls,
        Dictionary<int, ISyntaxNode[]> resolvedCallArgs = null,
        Dictionary<int, IFunctionSignature> resolvedCallSignatures = null) {
        GenericFunctionTypes = genericFunctionTypes;
        _bodyTypeSolving = bodyTypeSolving;
        _functionalVariables = functionalVariables;
        _recursiveCalls = recursiveCalls;
        _resolvedCallArgs = resolvedCallArgs;
        _resolvedCallSignatures = resolvedCallSignatures;
    }

    public IFunctionSignature GetFunctionalVariableOrNull(int id) =>
        _functionalVariables.Count <= id
            ? null
            : _functionalVariables[id];

    public ITicNodeState[] GetGenericCallArguments(int id) =>
        GenericFunctionTypes.Length <= id
            ? null
            : GenericFunctionTypes[id];

    public StateFun GetRecursiveCallOrNull(int id) =>
        _recursiveCalls.Count <= id
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
}