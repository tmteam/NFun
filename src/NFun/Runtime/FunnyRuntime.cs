using System;
using System.Collections.Generic;
using NFun.Interpretation;
using NFun.Interpretation.Nodes;
using NFun.Types;

namespace NFun.Runtime; 

public class FunnyRuntime {

    private readonly IList<Equation> _equations;
    internal readonly IReadonlyVariableDictionary VariableDictionary;

    internal FunnyRuntime(IList<Equation> equations, IReadonlyVariableDictionary variableDictionary, TypeBehaviour typeBehaviour) {
        TypeBehaviour = typeBehaviour;
        _equations = equations;
        VariableDictionary = variableDictionary;
        _variables = new Lazy<IReadOnlyList<IFunnyVar>>(() => VariableDictionary.GetAllAsArray());
    }

    public TypeBehaviour TypeBehaviour { get; }

    private readonly Lazy<IReadOnlyList<IFunnyVar>> _variables;
    /// <summary>
    /// All inputs and outputs of current runtime
    /// </summary>
    public IReadOnlyList<IFunnyVar> Variables => _variables.Value;
    /// <summary>
    /// Returns variable with given name (ignore case). Returns null if variable is not found
    /// </summary>
    public IFunnyVar this[string name] => VariableDictionary.GetOrNull(name);
    /// <summary>
    /// Calculate output variables based on the current values of the input variables
    /// </summary>
    public void Run() {
        foreach (var equation in _equations)
            equation.Run();
    }
    /// <summary>
    /// Creates deep copy of current runtime, that can be used in different thread
    /// </summary>
    public FunnyRuntime Clone()
    {
        var dictionaryCopy = VariableDictionary.Clone();
        var context = new CloneContext(dictionaryCopy);
        var equations = _equations.SelectToArray(e => e.Clone(context));
        return new FunnyRuntime(equations, dictionaryCopy, TypeBehaviour);
    }
}