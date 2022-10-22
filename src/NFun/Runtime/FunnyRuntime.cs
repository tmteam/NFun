using System;
using System.Collections.Generic;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.Types;

namespace NFun.Runtime; 

public class FunnyRuntime {

   
    internal FunnyRuntime(IList<Equation> equations, IReadonlyVariableDictionary variableDictionary, IReadOnlyList<IUserFunction> userFunctions, FunnyConverter converter) {
        UserFunctions = userFunctions;
        Converter = converter;
        _equations = equations;
        VariableDictionary = variableDictionary;
        _variables = new Lazy<IReadOnlyList<IFunnyVar>>(() => VariableDictionary.GetAllAsArray());
    }
    private readonly IList<Equation> _equations;
    private readonly Lazy<IReadOnlyList<IFunnyVar>> _variables;
    internal readonly IReadonlyVariableDictionary VariableDictionary;

    /// <summary>
    /// List of all user functions defined in the script
    /// </summary>
    public IReadOnlyList<IUserFunction> UserFunctions { get; }
    
    public FunnyConverter Converter { get; }

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
        //origin user functions are used here as they are used only in info puprposes. 
        return new FunnyRuntime(equations, dictionaryCopy, UserFunctions, Converter);
    }
}