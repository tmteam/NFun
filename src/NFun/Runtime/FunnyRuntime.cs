using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.Runtime {

public class FunnyRuntime {
    public TypeBehaviour TypeBehaviour { get; }
    internal IEnumerable<VariableUsages> GetInputVariableUsages() =>
        VariableDictionary.GetAllUsages().Where(u => !u.Source.IsOutput);

    private readonly IList<Equation> _equations;
    internal readonly VariableDictionary VariableDictionary;

    internal FunnyRuntime(IList<Equation> equations, VariableDictionary variableDictionary, TypeBehaviour typeBehaviour) {
        TypeBehaviour = typeBehaviour;
        _equations = equations;
        VariableDictionary = variableDictionary;
    }

    /// <summary>
    /// Returns variable with given name (ignore case). Returns null if variable is not found
    /// </summary>
    /// <param name="name"></param>
    public IFunnyVar this[string name]
    {
        get
        {
            if (!VariableDictionary.TryGetUsages(name, out var usage))
                return null;
            return usage?.Source;
        }
    }

    public IReadOnlyList<IFunnyVar> Variables => VariableDictionary.GetAllSources();

    public void Run() {
        foreach (var equation in _equations)
            equation.Run();
    }
}

}